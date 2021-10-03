using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MonotonicContext = HpTimeStamps.MonotonicStampContext;
using Duration = HpTimeStamps.Duration;
namespace LoggerLibrary
{
    using MonotonicStamp = HpTimeStamps.MonotonicTimeStamp<MonotonicContext>;
    using MonotonicSource = HpTimeStamps.MonotonicTimeStampUtil<MonotonicContext>;

    public static partial class CodeGenLogger
    {
        //public const string FilePath = "CodeGenLog.txt";
        public const string FilePath = @"L:\Desktop\CodeGenLog.txt";
        public static ICodeGenLogger Logger => TheLogger.Value;
        public static bool IsLoggerAlreadySet => TheLogger.IsSet;
        
        public static void SupplyAlternateLoggerOrThrow(ICodeGenLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (!TheLogger.TrySetAlternateValue(logger))
            {
                throw new InvalidOperationException("The logger has already been set.");
            }
        }

        public static void SupplyAlternateLoggingPathOrThrow(string fileName)
        {
            ICodeGenLogger logger = CodeGenLogImpl.CreateLogger(fileName);
            try
            {
                SupplyAlternateLoggerOrThrow(logger);
            }
            catch (Exception)
            {
                logger.Dispose();
                throw;
            }
        }

        static ICodeGenLogger InitLogger() => CodeGenLogImpl.CreateLogger(FilePath);

        private static readonly LocklessLazyWriteOnce<ICodeGenLogger> TheLogger = new LocklessLazyWriteOnce<ICodeGenLogger>(InitLogger);
    }

    #region Nested Impl Def
    partial class CodeGenLogger
    {
        private sealed class CodeGenLogImpl : ICodeGenLogger
        {
            internal static ICodeGenLogger CreateLogger(string fileName)
            {
                if (fileName == null)
                {
                    throw new ArgumentNullException(nameof(fileName));
                }
                CodeGenLogImpl? ret = null;
                try
                {
                    ret = new CodeGenLogImpl(fileName);
                    ret._t.Start(ret._cts.Token);
                    MonotonicStamp quitAfter = MonotonicSource.StampNow + Duration.FromSeconds(2);
                    while (MonotonicSource.StampNow <= quitAfter && !ret._threadStart.IsSet)
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(25));
                    }
                    Debug.WriteLine($"Log file at: [{ret._logFile.FullName}].");
                    return ret;
                }
                catch (Exception)
                {
                    ret?.Dispose();
                    throw;
                }

            }

            public bool IsGood => _threadStart.IsSet && !_threadEnd.IsSet && !_disposed.IsSet && !_faulted.IsSet;

            /// <inheritdoc />
            public event EventHandler<MonotonicStampedEventArgs>? Faulted;

            /// <inheritdoc />
            public event EventHandler<MonotonicStampedEventArgs>? ThreadStopped;
            
            public bool IsDisposed => _disposed.IsSet;

            /// <inheritdoc />
            [SuppressMessage("ReSharper", "ConstantNullCoalescingCondition")]
            public EntryExitLog CreateEel(string type, string method, string extraInfo)
            {
                return new EntryExitLog(type ?? "UNKNOWN TYPE", method ?? "UNKNOWN METHOD", extraInfo ?? "NO MESSAGE",
                    this);
            }

            /// <inheritdoc />
            public void Log(in LogMessage lm)
            {
                if (IsGood && !IsDisposed)
                {
                    try
                    {
                        _logCollection.Add(lm);
                    }
                    catch
                    {
                        //eat it
                    }
                }
            }

            /// <inheritdoc />
            public void LogMessage(string message)
            {
                LogMessage lm = LoggerLibrary.LogMessage.CreateMessageLog(message);
                Log(in lm);
            }

            /// <inheritdoc />
            public void LogError(string error)
            {
                LogMessage lm = LoggerLibrary.LogMessage.CreateErrorLog(error);
                Log(in lm);
            }

            /// <inheritdoc />
            public void LogException(Exception error)
            {
                LogMessage lm = LoggerLibrary.LogMessage.CreateExceptionLog(error);
                Log(in lm);
            }

            public void Dispose() => Dispose(true);

            private void Dispose(bool disposing)
            {
                if (_disposed.TrySet() && disposing)
                {
                    if (_threadStart.IsSet && !_threadEnd.IsSet)
                    {
                        _cts.Cancel();
                    }
                    _t.Join();
                    _cts.Dispose();
                    _logCollection.Dispose();
                    _eventPump.Dispose();
                }
            }
            private void ThreadLoop(object? ctObj)
            {
                try
                {
                    if (ctObj is CancellationToken token)
                    {
                        _threadStart.TrySet();
                        while (true)
                        {
                            LogMessage dequeued = _logCollection.Take(token);
                            Log(in dequeued, token);
                            token.ThrowIfCancellationRequested();
                        }
                    }
                }
                catch (OperationCanceledException)
                {

                }
                catch (Exception ex)
                {
                    if (_faulted.TrySet())
                    {
                        OnFaulted($"The logger has entered a faulted state and cannot be used.  Exception: [{ex}]");
                    }
                }
                
                finally
                {
                    if (_threadEnd.TrySet())
                    {
                        OnThreadStopped("The logger just shut down.");
                    }
                }
            }

            private void OnFaulted(string s)
            {
                MonotonicStampedEventArgs args = new MonotonicStampedEventArgs(s);
                bool doOnEventPump = _eventPump.IsGood;
                bool doOnThreadPool = !doOnEventPump;

                if (doOnEventPump)
                {
                    try
                    {
                        _eventPump.RaiseEvent(DoIt);
                    }
                    catch
                    {
                        doOnThreadPool = true;
                    }
                }

                if (doOnThreadPool)
                {
                    Task.Run(DoIt);
                }

                void DoIt() => Faulted?.Invoke(this, args);
            }

            private void OnThreadStopped(string s)
            {
                MonotonicStampedEventArgs args = new MonotonicStampedEventArgs(s);
                bool doOnEventPump = _eventPump.IsGood;
                bool doOnThreadPool = !doOnEventPump;

                if (doOnEventPump)
                {
                    try
                    {
                        _eventPump.RaiseEvent(DoIt);
                    }
                    catch
                    {
                        doOnThreadPool = true;
                    }
                }

                if (doOnThreadPool)
                {
                    Task.Run(DoIt);
                }

                void DoIt() => ThreadStopped?.Invoke(this, args);
            }

            private void Log(in LogMessage lm, CancellationToken token)
            {
                bool loggedIt = false;
                int currentLogAttempt = 1;
                string? text = Stringify(in lm);
                token.ThrowIfCancellationRequested();
                while (!loggedIt && currentLogAttempt++ <= _maxLogAttempts && text != null)
                {
                    try
                    {
                        using var sr = _logFile.AppendText();
                        sr.WriteLine(text);
                        loggedIt = true;
                        Debug.WriteLine(text);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(50));
                        token.ThrowIfCancellationRequested();
                    }
                }
            }

            private string? Stringify(in LogMessage lm)
            {
                try
                {
                    return (lm.TypeOfLog) switch
                    {
                        LogType.EntryExit => lm.Message,
                        LogType.Error => $"At {lm.EntryTime.ToString()}, the following error occurred: [{lm.Message}].",
                        LogType.Exception => $"At {lm.EntryTime.ToString()}, an exception of type " +
                                             $"{lm.ReportedException?.GetType().Name ?? "UNKNOWN"} with " +
                                             $"message \"{lm.ReportedException?.Message ?? "NONE"}\" was thrown.  " +
                                             $"Contents: [{lm.ReportedException?.ToString() ?? "NONE"}].",
                        _ => $"At {lm.EntryTime.ToString()}, the following message was posted: [{lm.Message}]."
                    };
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    return null;
                }
            }

            private CodeGenLogImpl(string fileName)
            {
                _logFile = new FileInfo(fileName);
                if (_logFile.Exists && _logFile.IsReadOnly)
                {
                    throw new IOException($"The log file specified {fileName} is readonly.");
                }

                _cts = new CancellationTokenSource();
                _logCollection = new BlockingCollection<LogMessage>(new ConcurrentQueue<LogMessage>());
                _t = new Thread(ThreadLoop)
                    { Name = "LogThread", IsBackground = true, Priority = ThreadPriority.BelowNormal };
                _eventPump = EventPumpFactorySource.FactoryInstance("LoggerEventPump");
            }

            private readonly IEventPump _eventPump;
            private readonly int _maxLogAttempts = 3;
            private readonly FileInfo _logFile;
            private LocklessSetOnlyRefStr _disposed;
            private readonly CancellationTokenSource _cts;
            private readonly Thread _t;
            private LocklessSetOnlyRefStr _threadStart;
            private LocklessSetOnlyRefStr _threadEnd;
            private LocklessSetOnlyRefStr _faulted;
            private readonly BlockingCollection<LogMessage> _logCollection;

           
        }

    } 
    #endregion

}
