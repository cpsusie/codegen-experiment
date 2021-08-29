using System;
using System.Collections.Generic;
using System.Text;
using MonotonicContext = HpTimeStamps.MonotonicStampContext;
namespace LoggerLibrary
{
    using MonotonicStamp = HpTimeStamps.MonotonicTimeStamp<MonotonicContext>;
    using MonoStampSource = HpTimeStamps.MonotonicTimeStampUtil<MonotonicContext>;
    
    public sealed class MonotonicStampedEventArgs : EventArgs, IEquatable<MonotonicStampedEventArgs>, IComparable<MonotonicStampedEventArgs>
    {
        public MonotonicStamp TimeStamp { get; }
        public string Message { get; }

        public MonotonicStampedEventArgs(string message, MonotonicStamp timeStamp)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            TimeStamp = timeStamp;
        }

        public MonotonicStampedEventArgs(string message) : this(message, MonoStampSource.StampNow) {}

        public bool Equals(MonotonicStampedEventArgs? other) =>
            TimeStamp == other?.TimeStamp && StringComparer.Ordinal.Equals(Message, other.Message);

        public int CompareTo(MonotonicStampedEventArgs? other)
        {
            if (ReferenceEquals(other, null)) return 1;
            int tsComp = TimeStamp.CompareTo(other.TimeStamp);
            return tsComp == 0 ? StringComparer.Ordinal.Compare(Message, other.Message) : tsComp;
        }

        public static bool operator ==(MonotonicStampedEventArgs? lhs, MonotonicStampedEventArgs? rhs) =>
            ReferenceEquals(lhs, rhs) || (lhs?.Equals(rhs) == true);
        public static bool operator !=(MonotonicStampedEventArgs? lhs, MonotonicStampedEventArgs? rhs)
            => !(lhs == rhs);
        public override int GetHashCode() => TimeStamp.GetHashCode();
        public override bool Equals(object? obj) => Equals(obj as MonotonicStampedEventArgs);
        public override string ToString() => $"At [" + TimeStamp.ToString() + $"]: event msg: \"{Message}\".";
    }
}
