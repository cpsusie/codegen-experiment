using System;
using System.Collections.Immutable;
using System.Linq;
using Cjm.Templates.Utilities;
using MonotonicContext = HpTimeStamps.MonotonicStampContext;
using PortableMonotonicStamp = HpTimeStamps.PortableMonotonicStamp;
namespace Cjm.Templates
{
    using MonotonicStamp = HpTimeStamps.MonotonicTimeStamp<MonotonicContext>;
    using StampSource = HpTimeStamps.MonotonicTimeStampUtil<MonotonicContext>;

    public sealed class TemplateInterfaceRecordsIdentifiedEventArgs : EventArgs, IEquatable<TemplateInterfaceRecordsIdentifiedEventArgs>
    {
        public ImmutableArray<FoundTemplateInterfaceRecord> IdentifiedTemplateInterfaceRecords { get; }
        public ref readonly PortableMonotonicStamp TimeStamp => ref _stamp;

        public TemplateInterfaceRecordsIdentifiedEventArgs(ImmutableArray<FoundTemplateInterfaceRecord> records, MonotonicStamp timeStamp)
        {
            _stamp = (PortableMonotonicStamp)timeStamp;
            IdentifiedTemplateInterfaceRecords = records.IsDefault
                ? throw new UninitializedStructArgumentException<ImmutableArray<FoundTemplateInterfaceRecord>>(
                    nameof(records))
                : records;
        }
        public TemplateInterfaceRecordsIdentifiedEventArgs(ImmutableArray<FoundTemplateInterfaceRecord> records) : this(records, StampSource.StampNow) {}

        public bool Equals(TemplateInterfaceRecordsIdentifiedEventArgs? other) => other?._stamp == _stamp &&
            other.IdentifiedTemplateInterfaceRecords.SequenceEqual(IdentifiedTemplateInterfaceRecords);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            int hash = _stamp.GetHashCode();
            unchecked
            {
                hash = (hash * 397) ^ IdentifiedTemplateInterfaceRecords.Length;
                switch (IdentifiedTemplateInterfaceRecords.Length)
                {
                    case 1:
                        hash = (hash * 397) ^ IdentifiedTemplateInterfaceRecords.First().GetHashCode();
                        break;
                    case > 1:
                        hash = (hash * 397) ^ IdentifiedTemplateInterfaceRecords.First().GetHashCode();
                        hash = (hash * 397) ^ IdentifiedTemplateInterfaceRecords.Last().GetHashCode();
                        break;
                }
            }
            return hash;
        }

        public override bool Equals(object? other) => Equals(other as TemplateInterfaceRecordsIdentifiedEventArgs);
        /// <inheritdoc />
        public override string ToString() => "At [" + _stamp.ToString() +
                                             $"], identified {IdentifiedTemplateInterfaceRecords.Length} template interface records.";
        public static bool operator ==(TemplateInterfaceRecordsIdentifiedEventArgs? lhs,
            TemplateInterfaceRecordsIdentifiedEventArgs? rhs) => ReferenceEquals(lhs, rhs) || lhs?.Equals(rhs) == true;
        public static bool operator !=(TemplateInterfaceRecordsIdentifiedEventArgs? lhs,
            TemplateInterfaceRecordsIdentifiedEventArgs? rhs) => !(lhs == rhs);

        private readonly PortableMonotonicStamp _stamp;
    }
}