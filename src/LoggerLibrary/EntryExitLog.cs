using System;
using MonotonicContext = HpTimeStamps.MonotonicStampContext;
using Duration = HpTimeStamps.Duration;
namespace LoggerLibrary
{
    using MonotonicStamp = HpTimeStamps.MonotonicTimeStamp<MonotonicContext>;
    using MonotonicSource = HpTimeStamps.MonotonicTimeStampUtil<MonotonicContext>;
    public struct EntryExitLog : IDisposable
    {
        public readonly MonotonicStamp StartTime => _entryTime;
        public readonly MonotonicStamp EndTime => _exitTime;

        public readonly Duration? Elapsed
        {
            get
            {
                Duration temp = EndTime - StartTime;
                return temp > Duration.Zero ? (Duration?)temp : null;
            }
        }

        public readonly string TypeName => _typeName ?? "UNKNOWN";
        public readonly string MethodName => _methodName ?? "UNKNOWN";
        public readonly string Message => _message ?? "NONE";
        
        public EntryExitLog(string typeName, string methodName, string message, ICodeGenLogger logger)
        {
            var now = MonotonicSource.StampNow;
            _typeName = typeName;
            _methodName = methodName;
            _message = message;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _disposed = default;
            _entryTime = now;
            _exitTime = now;
            _logger.Log(new LogMessage(this));
        }

        public void Dispose()
        {
            if (_disposed.TrySet())
            {
                _exitTime = MonotonicSource.StampNow;
                _logger?.Log(new LogMessage(in this));
            }
        }

        private readonly MonotonicStamp _entryTime;
        private readonly string? _message;
        private readonly string? _methodName;
        private readonly string? _typeName;
        private LocklessSetOnlyRefStr _disposed;
        private MonotonicStamp _exitTime;
        private readonly ICodeGenLogger? _logger;
    }
}
