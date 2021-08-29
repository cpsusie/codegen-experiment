using System;

namespace LoggerLibrary
{
    public delegate IEventPump EventPumpFactory(string threadName);

    public interface IEventPump : IDisposable
    {
        string ThreadName { get; }
        bool IsDisposed { get; }
        bool IsFaulted { get; }
        bool IsGood { get; }
        void RaiseEvent(Action eventToRaise);

    }
}
