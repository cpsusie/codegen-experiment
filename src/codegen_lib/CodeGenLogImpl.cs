using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using MonotonicContext = HpTimeStamps.MonotonicStampContext;
using Duration = HpTimeStamps.Duration;
namespace Cjm.CodeGen
{
    using MonotonicStamp = HpTimeStamps.MonotonicTimeStamp<MonotonicContext>;
    using MonotonicSource = HpTimeStamps.MonotonicTimeStampUtil<MonotonicContext>;

    internal static class CodeGenLogger
    {
        public const string FilePath = "CodeGenLog.txt";

        public static ICodeGenLogger Logger { get; } = CodeGenLogImpl.CreateLogger(FilePath);

        sealed class CodeGenLogImpl : ICodeGenLogger
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

            public bool IsGood => _threadStart.IsSet && !_threadEnd.IsSet && !_disposed.IsSet;

            public bool IsDisposed => _disposed.IsSet;

            /// <inheritdoc />
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
                LogMessage lm = CodeGen.LogMessage.CreateMessageLog(message);
                Log(in lm);
            }

            /// <inheritdoc />
            public void LogError(string error)
            {
                LogMessage lm = CodeGen.LogMessage.CreateErrorLog(error);
                Log(in lm);
            }

            /// <inheritdoc />
            public void LogException(Exception error)
            {
                LogMessage lm = CodeGen.LogMessage.CreateExceptionLog(error);
                Log(in lm);
            }

            private void ThrowIfDisposed()
            {
                if (_disposed.IsSet)
                    throw new ObjectDisposedException(nameof(CodeGenLogImpl));
            }

            private void ThrowIfFaulted()
            {
                if (_threadEnd.IsSet)
                    throw new InvalidOperationException("The thread is faulted.");
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
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    _threadEnd.TrySet();
                }
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
            }

            private readonly int _maxLogAttempts = 3;
            private readonly FileInfo _logFile;
            private LocklessSetOnlyRefStr _disposed;
            private readonly CancellationTokenSource _cts;
            private readonly Thread _t;
            private LocklessSetOnlyRefStr _threadStart;
            private LocklessSetOnlyRefStr _threadEnd;
            private readonly BlockingCollection<LogMessage> _logCollection;
        }
    }
    
}
