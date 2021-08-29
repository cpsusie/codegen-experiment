using System;
using System.Runtime.Serialization;
using MonotonicContext = HpTimeStamps.MonotonicStampContext;
namespace LoggerLibrary
{
  
    using MonotonicStamp = HpTimeStamps.MonotonicTimeStamp<MonotonicContext>;
    using MonoStampSource = HpTimeStamps.MonotonicTimeStampUtil<MonotonicContext>;
    using PortableMonotonicStamp = HpTimeStamps.PortableMonotonicStamp;
    [DataContract]
    public sealed class PortableMonotonicStampedEventArgs : EventArgs, IEquatable<PortableMonotonicStampedEventArgs>, IComparable<PortableMonotonicStampedEventArgs>
    {

        public ref readonly PortableMonotonicStamp TimeStamp => ref _timeStamp;
        [DataMember] public string Message { get; }

        public static explicit operator PortableMonotonicStampedEventArgs(MonotonicStampedEventArgs convertMe) =>
            new PortableMonotonicStampedEventArgs(
                (convertMe ?? throw new ArgumentNullException(nameof(convertMe))).Message,
                (PortableMonotonicStamp)convertMe.TimeStamp);
        
        public PortableMonotonicStampedEventArgs(string message, MonotonicStamp timeStamp) 
            : this(message, (PortableMonotonicStamp)timeStamp) {}
        public PortableMonotonicStampedEventArgs(string message) : this(message, MonoStampSource.StampNow) { }
        
        public PortableMonotonicStampedEventArgs(string message, PortableMonotonicStamp timeStamp)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            _timeStamp = timeStamp;
        }
        
        public bool Equals(PortableMonotonicStampedEventArgs? other) =>
            TimeStamp == other?.TimeStamp && StringComparer.Ordinal.Equals(Message, other.Message);

        public int CompareTo(PortableMonotonicStampedEventArgs? other)
        {
            if (ReferenceEquals(other, null)) return 1;
            int tsComp = PortableMonotonicStamp.Compare(in _timeStamp, in other._timeStamp);
            return tsComp == 0 ? StringComparer.Ordinal.Compare(Message, other.Message) : tsComp;
        }

        public static bool operator ==(PortableMonotonicStampedEventArgs? lhs, PortableMonotonicStampedEventArgs? rhs) =>
            ReferenceEquals(lhs, rhs) || (lhs?.Equals(rhs) == true);
        public static bool operator !=(PortableMonotonicStampedEventArgs? lhs, PortableMonotonicStampedEventArgs? rhs)
            => !(lhs == rhs);
        public override int GetHashCode() => TimeStamp.GetHashCode();
        public override bool Equals(object? obj) => Equals(obj as PortableMonotonicStampedEventArgs);
        public override string ToString() => $"At [" + TimeStamp.ToString() + $"]: event msg: \"{Message}\".";

        [OnDeserialized]
        private void OnDeserializedMethod(StreamingContext context)
        {
            if (Message == null)
                throw new SerializationException(
                    "The deserialized object contains an invalid null reference instead of a message string.");
        }


        [DataMember] private readonly PortableMonotonicStamp _timeStamp;
    }
}
