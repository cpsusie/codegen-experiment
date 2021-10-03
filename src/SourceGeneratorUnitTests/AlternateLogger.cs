using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using LoggerLibrary;
using Xunit.Abstractions;
using MonotonicContext = HpTimeStamps.MonotonicStampContext;
using Duration = HpTimeStamps.Duration;
[assembly: InternalsVisibleTo("Cjm.Templates.Test")]
namespace SourceGeneratorUnitTests
{
    using MonotonicStamp = HpTimeStamps.MonotonicTimeStamp<MonotonicContext>;
    using MonotonicSource = HpTimeStamps.MonotonicTimeStampUtil<MonotonicContext>;
    public static class AlternateLoggerSource
    {
        public static ICodeGenLogger CreateAlternateLogger(ITestOutputHelper helper)
        {
            return AlternateLogger.CreateLogger(helper ?? throw new ArgumentNullException(nameof(helper)));
        }

        public static void InjectAlternateLogger(ITestOutputHelper helper)
        {
            ICodeGenLogger? logger = null;
            try
            {
                if (_setOnce.TrySet())
                {
                    logger = AlternateLogger.CreateLogger(helper);
                    CodeGenLogger.SupplyAlternateLoggerOrThrow(logger!);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                logger?.Dispose();
                
            }
        }

        sealed class AlternateLogger : ICodeGenLogger
        {
            internal static ICodeGenLogger CreateLogger([JetBrains.Annotations.NotNull] ITestOutputHelper helper)
            {
                if (helper == null) throw new ArgumentNullException(nameof(helper));
                AlternateLogger? ret = null;
                try
                {
                    ret = new AlternateLogger(helper);
                    ret._t.Start(ret._cts.Token);
                    MonotonicStamp quitAfter = MonotonicSource.StampNow + Duration.FromSeconds(2);
                    while (MonotonicSource.StampNow <= quitAfter && !ret._threadStart.IsSet)
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(25));
                    }
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
                        _helper.WriteLine(text!);
                        loggedIt = true;
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

            private AlternateLogger(ITestOutputHelper helper)
            {
                _helper = helper ?? throw new ArgumentNullException(nameof(helper));
                _cts = new CancellationTokenSource();
                _logCollection = new BlockingCollection<LogMessage>(new ConcurrentQueue<LogMessage>());
                _t = new Thread(ThreadLoop)
                { Name = "LogThread", IsBackground = true, Priority = ThreadPriority.BelowNormal };
                _eventPump = EventPumpFactorySource.FactoryInstance("LoggerEventPump");
            }

            private readonly ITestOutputHelper _helper;
            private readonly IEventPump _eventPump;
            private readonly int _maxLogAttempts = 3;
            private LocklessSetOnlyRefStr _disposed;
            private readonly CancellationTokenSource _cts;
            private readonly Thread _t;
            private LocklessSetOnlyRefStr _threadStart;
            private LocklessSetOnlyRefStr _threadEnd;
            private LocklessSetOnlyRefStr _faulted;
            private readonly BlockingCollection<LogMessage> _logCollection;


        }

        private static LocklessSetOnlyFlag _setOnce;
    }

    
}
