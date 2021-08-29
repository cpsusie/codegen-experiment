using System;
using MonotonicContext = HpTimeStamps.MonotonicStampContext;
using Duration = HpTimeStamps.Duration;
namespace LoggerLibrary
{
    using MonotonicStamp = HpTimeStamps.MonotonicTimeStamp<MonotonicContext>;
    using MonotonicSource = HpTimeStamps.MonotonicTimeStampUtil<MonotonicContext>;
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
            TimePeriod = lm.Elapsed ?? Duration.Zero;
            _message = TimePeriod == null ? $"{EntryTime.ToString()} ENTRY\t\tType: {lm.TypeName}\tMethod: {lm.MethodName}\tMessage: {lm.Message}." : $"{lm.EndTime.ToString()} EXIT\t\tType: {lm.TypeName}\tMethod: {lm.MethodName}\tDuration: {TimePeriod.Value.TotalMilliseconds:N4} milliseconds.";
            TypeOfLog = LogType.EntryExit;
        }

        private LogMessage(string? message, Exception? exception, bool error)
        {
            EntryTime = MonotonicSource.StampNow;
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
}