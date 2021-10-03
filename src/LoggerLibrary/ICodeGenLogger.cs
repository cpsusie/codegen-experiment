using System;
using MonotonicContext = HpTimeStamps.MonotonicStampContext;
namespace LoggerLibrary
{
    using MonotonicStamp = HpTimeStamps.MonotonicTimeStamp<MonotonicContext>;
    using MonoStampSource = HpTimeStamps.MonotonicTimeStampUtil<MonotonicContext>;

    public interface ICodeGenLogger : IDisposable
    {
        event EventHandler<MonotonicStampedEventArgs> Faulted;
        event EventHandler<MonotonicStampedEventArgs> ThreadStopped; 
        bool IsDisposed { get; }
        bool IsGood { get; }
        EntryExitLog CreateEel(string type, string method, string extraInfo);
        void Log(in LogMessage lm);
        void LogMessage(string message);
        void LogError(string error);
        void LogException(Exception error);
    }

    public interface IWrap<T> where T : notnull
    {
        T CurrentSetting { get; }

        T Update(T value);
    }
}
