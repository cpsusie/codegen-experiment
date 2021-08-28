using System;
using System.Diagnostics;
using System.Threading;
using HpTimeStamps;
using MonotonicContext = HpTimeStamps.MonotonicStampContext;
namespace Cjm.CodeGen
{
    using MonotonicStamp = HpTimeStamps.MonotonicTimeStamp<MonotonicContext>;
    using MonoStampSource = HpTimeStamps.MonotonicTimeStampUtil<MonotonicContext>;

    public interface ICodeGenLogger : IDisposable
    {
        bool IsDisposed { get; }
        bool IsGood { get; }
        EntryExitLog CreateEel(string type, string method, string extraInfo);
        void Log(in LogMessage lm);
        void LogMessage(string message);
        void LogError(string error);
        void LogException(Exception error);
    }

    public readonly struct LogMessage : IEquatable<LogMessage>, IComparable<LogMessage>
    {
        public static LogMessage CreateMessageLog(string message) =>
            new LogMessage(message ?? throw new ArgumentNullException(nameof(message)), null, false);

        public static LogMessage CreateErrorLog(string error) =>
            new LogMessage(error ?? throw new ArgumentNullException(nameof(error)), null, true);

        public static LogMessage CreateExceptionLog(Exception ex) =>
            new LogMessage(null, ex ?? throw new ArgumentNullException(nameof(ex)), true);

        public MonotonicStamp EntryTime { get; }
        public Duration? TimePeriod { get; }
        public string Message => _message ?? "NO MESSAGE";
        public LogType TypeOfLog { get; }
        public Exception? ReportedException => _exception;
        public LogMessage(in EntryExitLog lm)
        {
            _exception = null;
            EntryTime = lm.StartTime;
            TimePeriod = lm.Elapsed;
            _message = TimePeriod == null ? $"{EntryTime.ToString()} ENTRY\t\tType: {lm.TypeName}\tMethod: {lm.MethodName}\tMessage: {lm.Message}." : $"{lm.EndTime.ToString()} EXIT\t\tType: {lm.TypeName}\tMethod: {lm.MethodName}\tDuration: {TimePeriod.Value.TotalMilliseconds:N4} milliseconds.";
            TypeOfLog = LogType.EntryExit;
        }

        private LogMessage(string? message, Exception? exception, bool error)
        {
            EntryTime = MonoStampSource.StampNow;
            TimePeriod = null;
            _message = message ?? $"NO {(error ? "ERROR" : "MESSAGE")} PAYLOAD";
            TypeOfLog = (exception, error) switch
            {
                ({ } e, _) => LogType.Exception,
                (_, true) => LogType.Error,
                _ => LogType.Message,
            };
            _exception = exception;
        }

        public static int Compare(in LogMessage lhs, in LogMessage rhs)
        {
            int ret;
            int startComp = lhs.EntryTime.CompareTo(rhs.EntryTime);
            if (startComp == 0)
            {
                int durComp = CompareDurations(lhs.TimePeriod, rhs.TimePeriod);
                if (durComp == 0)
                {
                    int ltComp = CompareType(lhs.TypeOfLog, rhs.TypeOfLog);
                    ret = ltComp == 0 ? TheStringComparer.Compare(lhs.Message, rhs.Message) : ltComp;
                }
                else
                {
                    ret = durComp;
                }
            }
            else
            {
                ret = startComp;
            }
            return ret;
        }

        public static bool operator ==(in LogMessage lhs, in LogMessage rhs) => lhs.EntryTime == rhs.EntryTime &&
            lhs.TimePeriod == rhs.TimePeriod && lhs.TypeOfLog == rhs.TypeOfLog &&
            TheStringComparer.Equals(lhs.Message, rhs.Message);
        public static bool operator !=(in LogMessage lhs, in LogMessage rhs) => !(lhs == rhs);
        public static bool operator >(in LogMessage lhs, in LogMessage rhs) => Compare(in lhs, in rhs) > 0;
        public static bool operator <(in LogMessage lhs, in LogMessage rhs) => Compare(in lhs, in rhs) < 0;
        public static bool operator >=(in LogMessage lhs, in LogMessage rhs) => !(lhs < rhs);
        public static bool operator <=(in LogMessage lhs, in LogMessage rhs) => !(lhs > rhs);

        /// <inheritdoc />
        public override int GetHashCode() => EntryTime.GetHashCode();

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is LogMessage lm && lm == this;

        public bool Equals(LogMessage other) => other == this;
        public int CompareTo(LogMessage other) => Compare(in this, in other);
        
        private static int CompareType(LogType l, LogType r)
        {
            if (l == r) return 0;
            return l > r ? 1 : -1;
        }


        private static int CompareDurations(Duration? l, Duration? r)
        {
            if (!l.HasValue && !r.HasValue) return 0;
            if (l.HasValue != r.HasValue)
            {
                return l.HasValue ? 1 : -1;
            }

            return Duration.Compare(l!.Value, r!.Value);
        }

        private readonly string? _message;
        private readonly Exception? _exception;
        private static readonly TrimmedStringComparer TheStringComparer = TrimmedStringComparer.TrimmedOrdinalIgnoreCase;
    }

    public enum LogType
    {
        Message=0,
        Error,
        Exception,
        EntryExit

    }

    public struct EntryExitLog : IDisposable
    {
        public readonly MonotonicStamp StartTime => _entryTime;
        public readonly MonotonicStamp EndTime => _exitTime;

        public readonly Duration? Elapsed
        {
            get
            {
                Duration temp = EndTime - StartTime;
                return temp > Duration.Zero ? temp : null;
            }
        }

        public readonly string TypeName => _typeName ?? "UNKNOWN";
        public readonly string MethodName => _methodName ?? "UNKNOWN";
        public readonly string Message => _message ?? "NONE";
        internal EntryExitLog(string typeName, string methodName, string message, ICodeGenLogger logger)
        {
            _typeName = typeName;
            _methodName = methodName;
            _message = message;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _disposed = default;
            _entryTime = MonoStampSource.StampNow;
            _exitTime = MonoStampSource.StampNow;
            _logger.Log(new LogMessage(in this));
        }

        public void Dispose()
        {
            if (_disposed.TrySet())
            {
                _exitTime = MonoStampSource.StampNow;
                _logger?.Log(new LogMessage(in this));
            }
        }

        private readonly MonotonicStamp _entryTime;
        private string? _message;
        private string? _methodName;
        private string? _typeName;
        private LocklessSetOnlyRefStr _disposed;
        private MonotonicStamp _exitTime;
        private readonly ICodeGenLogger? _logger;
    }

    internal static class TrimmedStringComparers
    {

    }

    internal sealed class TrimmedStringComparer : StringComparer
    {

        public static TrimmedStringComparer TrimmedOrdinal => TheOrdinalComparer.Value;

        public static TrimmedStringComparer TrimmedOrdinalIgnoreCase => TheOrdinalIgnoreCaseComparer.Value;

        /// <inheritdoc />
        public override int Compare(string? x, string? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(x, null)) return 1;
            if (ReferenceEquals(y, null)) return -1;
            return x.AsSpan().Trim().CompareTo(y.AsSpan().Trim(), _baseComparison);
        }

        /// <inheritdoc />
        public override bool Equals(string? x, string? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) return false;
            return x.AsSpan().Trim().Equals(y.AsSpan().Trim(), _baseComparison);
        }

        /// <inheritdoc />
        public override int GetHashCode(string? obj)
        {
            obj = obj?.Trim() ?? string.Empty;
            return BaseComparer.GetHashCode(obj);
        }

        private StringComparer BaseComparer => _baseComparison switch
        {
            StringComparison.Ordinal => Ordinal,
            StringComparison.OrdinalIgnoreCase => OrdinalIgnoreCase,
            StringComparison.InvariantCulture => InvariantCulture,
            StringComparison.InvariantCultureIgnoreCase => InvariantCultureIgnoreCase,
            StringComparison.CurrentCulture => CurrentCulture,
            StringComparison.CurrentCultureIgnoreCase => CurrentCultureIgnoreCase,
            _ => Ordinal
        };

        private TrimmedStringComparer(StringComparison baseComp) => _baseComparison = baseComp;

        private readonly StringComparison _baseComparison;

        private static readonly LocklessLazyWriteOnce<TrimmedStringComparer> TheOrdinalComparer =
            new LocklessLazyWriteOnce<TrimmedStringComparer>(() => InitComparer(StringComparison.Ordinal));
        private static readonly LocklessLazyWriteOnce<TrimmedStringComparer> TheOrdinalIgnoreCaseComparer = new LocklessLazyWriteOnce<TrimmedStringComparer>(
            () => InitComparer(StringComparison.OrdinalIgnoreCase));
        private static readonly LocklessLazyWriteOnce<TrimmedStringComparer> TheCurrentCultureComparer =
            new LocklessLazyWriteOnce<TrimmedStringComparer>(() =>
                new TrimmedStringComparer(StringComparison.CurrentCulture));
        private static readonly LocklessLazyWriteOnce<TrimmedStringComparer> TheCurrentCultureIgnoreCaseComparer =
            new LocklessLazyWriteOnce<TrimmedStringComparer>(() =>
                new TrimmedStringComparer(StringComparison.CurrentCultureIgnoreCase));

        private static readonly LocklessLazyWriteOnce<TrimmedStringComparer> TheInvariantCultureComparer =
            new LocklessLazyWriteOnce<TrimmedStringComparer>(() =>
                new TrimmedStringComparer(StringComparison.InvariantCulture));
        private static readonly LocklessLazyWriteOnce<TrimmedStringComparer> TheInvariantCultureIgnoreCaseComparer =
            new LocklessLazyWriteOnce<TrimmedStringComparer>(() =>
                new TrimmedStringComparer(StringComparison.InvariantCultureIgnoreCase));
        


        private static TrimmedStringComparer InitComparer(StringComparison baseComp) =>
            new TrimmedStringComparer(baseComp);
    }

    internal struct LocklessSetOnlyRefStr
    {
        public bool IsSet
        {
            get
            {
                int val = _value;
                return val == Set;
            }
        }

        public bool TrySet()
        {
            const int wantToBe = Set;
            const int needToBeNow = Clear;
            return Interlocked.CompareExchange(ref _value, wantToBe, needToBeNow) == needToBeNow;
        }

        private volatile int _value;
        private const int Clear = 0;
        private const int Set = 1;
    }

    internal sealed class LocklessLazyWriteOnce<T> where T : class
    {
        public T Value
        {
            get
            {
                T? ret = _value;
                
                if (ret == null)
                {
                    bool iSwappedIt = false;
                    bool ok;
                    T? newVal;
                    do
                    {
                        (ok, newVal) = Generate();
                        ret = _value;
                    } while (ret == null && (!ok || newVal == null));

                    if (ret == null)
                    {
                        iSwappedIt = Interlocked.CompareExchange(ref _value, newVal, null) == null;
                        ret = _value;
                    }

                    if (iSwappedIt)
                    {
                        Interlocked.Exchange(ref _init, null);
                    }
                }
                Debug.Assert(ret != null);
                return ret!;
            }
        }

        public bool IsSet
        {
            get
            {
                T? ret = _value;
                return ret != null;
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            bool isSet = IsSet;
            return isSet ? $"[{nameof(LocklessLazyWriteOnce<T>)}]-- Value: {Value}" : $"[{nameof(LocklessLazyWriteOnce<T>)}]-- NOT SET";
        }

        internal LocklessLazyWriteOnce(Func<T> initializer)
            => _init = initializer ?? throw new ArgumentNullException(nameof(initializer));
        private (bool Ok,  T? Value) Generate()
        {
            try
            {
                Func<T>? init = _init;
                T? ret = init?.Invoke();
                return (ret != null, ret);
            }
            catch (Exception)
            {
                return (false, null);
            }
        }

        private volatile T? _value;
        private volatile Func<T>? _init;
    }
}
