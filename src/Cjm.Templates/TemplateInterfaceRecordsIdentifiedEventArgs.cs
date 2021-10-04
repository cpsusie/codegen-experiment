using System;
using System.Collections.Immutable;
using System.Linq;
using Cjm.Templates.Utilities;
using Cjm.Templates.Utilities.SetOnce;
using MonotonicContext = HpTimeStamps.MonotonicStampContext;
using PortableMonotonicStamp = HpTimeStamps.PortableMonotonicStamp;
namespace Cjm.Templates
{
    using MonotonicStamp = HpTimeStamps.MonotonicTimeStamp<MonotonicContext>;
    using StampSource = HpTimeStamps.MonotonicTimeStampUtil<MonotonicContext>;

    public abstract class TemplateRecordIdentifiedEventArgs : EventArgs, IEquatable<TemplateRecordIdentifiedEventArgs>
    {
        #region Properties
        public ref readonly PortableMonotonicStamp TimeStamp => ref _stamp;
        protected Type ConcreteType => _concreteType.ConcreteType;
        protected string ConcreteTypeName => _concreteType.ConcreteTypeName; 
        #endregion

        #region CTORS
        protected TemplateRecordIdentifiedEventArgs() : this(StampSource.StampNow) { }
        protected TemplateRecordIdentifiedEventArgs(MonotonicStamp stamp)
            : this((PortableMonotonicStamp)stamp) { }
        protected TemplateRecordIdentifiedEventArgs(in PortableMonotonicStamp stamp)
        {
            _stamp = stamp;
            _concreteType = new LocklessConcreteType(this);
        } 
        #endregion

        #region Public Methods and Operators
        public sealed override int GetHashCode()
        {
            int hash = _stamp.GetHashCode();
            unchecked
            {
                hash = (hash * 397) ^ ConcreteType.GetHashCode();
                hash = (hash * 397) ^ GetImplSpecHash();
            }
            return hash;
        }

        /// <inheritdoc />
        public sealed override bool Equals(object? obj) => Equals(obj as TemplateRecordIdentifiedEventArgs);
        /// <inheritdoc />
        public sealed override string ToString() =>
            $"Timestamp: \t[{_stamp.ToString()}]; \tArgType: \t\"{ConcreteTypeName}\"; \tPayload: \t\"{GetImplSpecificStringRep()}\".";
        /// <inheritdoc />
        public bool Equals(TemplateRecordIdentifiedEventArgs? other) => other?.ConcreteType == ConcreteType &&
                                                                        other._stamp == _stamp && IsEqualToImpl(other);
        public static bool operator
            ==(TemplateRecordIdentifiedEventArgs? lhs, TemplateRecordIdentifiedEventArgs? rhs) =>
            ReferenceEquals(lhs, rhs) || lhs?.Equals(rhs) == true;
        public static bool operator
            !=(TemplateRecordIdentifiedEventArgs? lhs, TemplateRecordIdentifiedEventArgs? rhs) => !(lhs == rhs); 
        #endregion


        #region Protected Abstract Methods
        protected abstract bool IsEqualToImpl(TemplateRecordIdentifiedEventArgs other);
        protected abstract int GetImplSpecHash();
        protected abstract string GetImplSpecificStringRep(); 
        #endregion

        #region Privates
        private readonly PortableMonotonicStamp _stamp;
        private readonly LocklessConcreteType _concreteType; 
        #endregion
    }

    public sealed class TemplateRecordsIdentifiedEventArgs<TPayload> : TemplateRecordIdentifiedEventArgs,
        IEquatable<TemplateRecordsIdentifiedEventArgs<TPayload>> where TPayload : IEquatable<TPayload>
    {
        public ImmutableArray<TPayload> IdentifiedTemplateRecords { get; }

        /// <inheritdoc />
        public TemplateRecordsIdentifiedEventArgs(ImmutableArray<TPayload> identifiedTemplateInterfaceRecords) 
            : this(StampSource.StampNow, identifiedTemplateInterfaceRecords) {}
        

        /// <inheritdoc />
        public TemplateRecordsIdentifiedEventArgs(MonotonicStamp stamp, ImmutableArray<TPayload> identifiedTemplateInterfaceRecords) 
            : this((PortableMonotonicStamp) stamp, identifiedTemplateInterfaceRecords){}
        

        /// <inheritdoc />
        private TemplateRecordsIdentifiedEventArgs(in PortableMonotonicStamp stamp,
            ImmutableArray<TPayload> identifiedTemplateInterfaceRecords) : base(in stamp) =>
            IdentifiedTemplateRecords =
                identifiedTemplateInterfaceRecords.ValueOrThrowIfDefault(nameof(identifiedTemplateInterfaceRecords));

        /// <inheritdoc />
        protected override bool IsEqualToImpl(TemplateRecordIdentifiedEventArgs other) => other is TemplateRecordsIdentifiedEventArgs<TPayload> opp && Equals(opp);

        /// <inheritdoc />
        protected override int GetImplSpecHash()
        {
            int hash = IdentifiedTemplateRecords.Length;
            unchecked
            {
                switch (IdentifiedTemplateRecords.Length)
                {
                    case 1:
                        hash = (hash * 397) ^ IdentifiedTemplateRecords.First().GetHashCode();
                        break;
                    case > 1:
                        hash = (hash * 397) ^ IdentifiedTemplateRecords.First().GetHashCode();
                        hash = (hash * 397) ^ IdentifiedTemplateRecords.Last().GetHashCode();
                        break;
                }
            }
            return hash;

        }

        /// <inheritdoc />
        protected override string GetImplSpecificStringRep() =>
            $" identified {IdentifiedTemplateRecords.Length} records of type {typeof(TPayload).Name}.";
        

        /// <inheritdoc />
        public bool Equals(TemplateRecordsIdentifiedEventArgs<TPayload> other) =>
            other.IdentifiedTemplateRecords.SequenceEqual(IdentifiedTemplateRecords);

    }

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