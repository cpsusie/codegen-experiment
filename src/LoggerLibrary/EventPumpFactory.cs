using System;
using System.Collections.Concurrent;
using System.Threading;
using MonotonicContext = HpTimeStamps.MonotonicStampContext;
using Duration = HpTimeStamps.Duration;
namespace LoggerLibrary
{
    using MonotonicStamp = HpTimeStamps.MonotonicTimeStamp<MonotonicContext>;
    using MonoStampSource = HpTimeStamps.MonotonicTimeStampUtil<MonotonicContext>;

    public static partial class EventPumpFactorySource
    {
        public static bool IsFactoryAlreadySet => TheFactory.IsSet;
        public static EventPumpFactory FactoryInstance => TheFactory.Value;

        public static bool TrySupplyAlternateEventPumpFactory(EventPumpFactory alternate) =>
            TheFactory.TrySetAlternateValue(alternate ?? throw new ArgumentNullException(nameof(alternate)));


        private static readonly LocklessLazyWriteOnce<EventPumpFactory> TheFactory =
            new LocklessLazyWriteOnce<EventPumpFactory>(() => DefaultEventPumpFactory);

        private static EventPumpFactory DefaultEventPumpFactory => EventPumpImpl.CreateEventPump;
    }

    #region Default Impl Nested TypeDef
    partial class EventPumpFactorySource
    {
        private sealed class EventPumpImpl : IEventPump
        {
            public static IEventPump CreateEventPump(string threadName)
            {
                var pump = new EventPumpImpl(threadName ?? throw new ArgumentNullException(nameof(threadName)));
                try
                {
                    pump._t.Start(pump._cts.Token);
                    MonotonicStamp timeoutAt = Duration.FromSeconds(2) + MonoStampSource.StampNow;
                    while (!pump.IsGood && MonoStampSource.StampNow <= timeoutAt)
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(50));
                    }

                    if (!pump.IsGood)
                    {
                        throw new InvalidOperationException(
                            "Was unable to confirm that the event pump thread started ok.");
                    }
                }
                catch (Exception)
                {
                    pump.Dispose();
                    throw;
                }

                return pump;
            }


            /// <inheritdoc />
            public string ThreadName => _t.Name ?? "UNKNOWN NAME";
            public bool IsDisposed => _disposed.IsSet;
            public bool IsFaulted => _faulted.IsSet;
            public bool IsGood => _threadStarted.IsSet && !_threadEnded.IsSet && !_faulted.IsSet && !_disposed.IsSet;
            private bool ShouldStopThread => _threadStarted.IsSet && !_threadEnded.IsSet;

            private EventPumpImpl(string threadName)
            {
                if (threadName == null) throw new ArgumentNullException(nameof(threadName));
                _actions = new BlockingCollection<Action>(new ConcurrentQueue<Action>());
                _t = new Thread(ThreadLoop)
                { IsBackground = true, Priority = ThreadPriority.BelowNormal, Name = threadName };
            }

            public void RaiseEvent(Action a)
            {
                if (IsGood)
                {
                    try
                    {
                        _actions.Add(a ?? throw new ArgumentNullException(nameof(a)));
                    }
                    catch (ArgumentNullException)
                    {
                        throw;
                    }
                    catch (Exception)
                    {
                        _faulted.TrySet();
                        throw;
                    }
                }
            }

            public void Dispose() => Dispose(true);

            private void Dispose(bool disposing)
            {
                if (_disposed.TrySet() && disposing)
                {
                    if (ShouldStopThread)
                    {
                        _cts.Cancel();
                    }
                    _t.Join();
                    _cts.Dispose();
                    _actions.Dispose();

                }
            }

            private void ThreadLoop(object? cancellationTokenObj)
            {
                try
                {
                    if (cancellationTokenObj is CancellationToken token)
                    {
                        _threadStarted.TrySet();

                        while (true)
                        {
                            Action a = _actions.Take(token);
                            ExecuteAction(a);
                        }
                    }
                }
                catch (OperationCanceledException)
                {

                }
                catch (Exception)
                {
                    _faulted.TrySet();
                    throw;
                }
                finally
                {
                    _threadEnded.TrySet();
                }
            }

            private void ExecuteAction(Action a)
            {
                try
                {
                    a();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Delegate added to event pump named [{_t.Name}] threw an exception of type [{ex.GetType().Name}] with message \"{ex.Message}\".");
                }
            }

            private readonly Thread _t;
            private LocklessSetOnlyRefStr _threadStarted;
            private LocklessSetOnlyRefStr _threadEnded;
            private LocklessSetOnlyRefStr _faulted;
            private LocklessSetOnlyRefStr _disposed;
            private readonly CancellationTokenSource _cts = new CancellationTokenSource();
            private readonly BlockingCollection<Action> _actions;

            
        }
    } 
    #endregion
}
