using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonotonicContext = HpTimeStamps.MonotonicStampContext;

namespace Cjm.CodeGen
{
    using MonotonicStamp = HpTimeStamps.MonotonicTimeStamp<MonotonicContext>;
    using StampSource = LoggerLibrary.TimeStampProvider;

    public abstract class GeneratorTestingPayloadEventArgs : EventArgs, IComparable<GeneratorTestingPayloadEventArgs>, IEquatable<GeneratorTestingPayloadEventArgs>
    {
        protected abstract Type PayloadType { get; }
        protected Type ConcreteType => _concreteType.ConcreteType;
        protected string ConcreteTypeName => _concreteType.ConcreteTypeName;

        protected GeneratorTestingPayloadEventArgs()
        {
            _stamp = StampSource.MonoNow;
            _concreteType = new LocklessConcreteType(this);
        }

        public bool Equals(GeneratorTestingPayloadEventArgs? value)
        {
            if (ReferenceEquals(value, null)) return false;
            return value.ConcreteType == ConcreteType && IsEqualTo(value);
        }

        public int CompareTo(GeneratorTestingPayloadEventArgs? args)
        {
            if (ReferenceEquals(this, args)) return 0;
            if (ReferenceEquals(args, null)) return 1;

            return _stamp.CompareTo(args._stamp);
        }

        public sealed override int GetHashCode()
        {
            int hash = _stamp.GetHashCode();
            unchecked
            {
                hash = (hash * 397) ^ ConcreteType.GetHashCode();
                hash = (hash * 397) ^ ExecuteGetHashCode();
            }
            return hash;
        }

        /// <inheritdoc />
        public sealed override bool Equals(object? obj) => Equals(obj as GeneratorTestingPayloadEventArgs);
        /// <inheritdoc />
        public sealed override string ToString() => $"[{ConcreteTypeName}] -- Timestamp: [" + _stamp.ToString() + "]: " + ExecuteGetStringRep();
        public static bool operator ==(GeneratorTestingPayloadEventArgs? lhs, GeneratorTestingPayloadEventArgs? rhs) => ReferenceEquals(lhs, rhs) || lhs?.Equals(rhs) == true;
        public static bool operator !=(GeneratorTestingPayloadEventArgs? lhs, GeneratorTestingPayloadEventArgs? rhs) =>
            !(lhs == rhs);

        protected abstract bool IsEqualTo(GeneratorTestingPayloadEventArgs other);
        protected abstract int ExecuteGetHashCode();
        protected abstract string ExecuteGetStringRep();

        private readonly MonotonicStamp _stamp;
        private readonly LocklessConcreteType _concreteType;
    }

    public abstract class
        GeneratorTestingPayloadEventArgs<TPayload, TPayloadComparer> : GeneratorTestingPayloadEventArgs
        where TPayloadComparer : unmanaged, IByRoRefEqualityComparer<TPayload>
        where TPayload : struct, IEquatable<TPayload>, IHasGenericByRefRoEqComparer<TPayloadComparer, TPayload>
    {
        public ref readonly TPayload Payload => ref _payload;

        protected GeneratorTestingPayloadEventArgs(in TPayload payload)
            => _payload = payload;

        private readonly TPayload _payload;
    }

    public sealed class GeneratorTestEnableAugmentSyntaxReceiverPayloadEventArgs : GeneratorTestingPayloadEventArgs<
        EnableAugmentedEnumerationExtensionTargetData, EnableAugmentedEnumerationExtensionTargetData.EqComp>
    {
        /// <inheritdoc />
        public GeneratorTestEnableAugmentSyntaxReceiverPayloadEventArgs(
            in EnableAugmentedEnumerationExtensionTargetData payload) : base(in payload)
        {
        }

        /// <inheritdoc />
        protected override Type PayloadType => typeof(EnableAugmentedEnumerationExtensionTargetData);


        /// <inheritdoc />
        protected override bool IsEqualTo(GeneratorTestingPayloadEventArgs other)
        {
            return other is GeneratorTestEnableAugmentSyntaxReceiverPayloadEventArgs a &&
                   TheComparer.Equals(in Payload, in a.Payload);
        }

        /// <inheritdoc />
        protected override int ExecuteGetHashCode()
        {
            return TheComparer.GetHashCode(in Payload);
        }

        /// <inheritdoc />
        protected override string ExecuteGetStringRep()
        {
            return $"payload type [{PayloadType.Name}] with value: [" + Payload.ToString() + "].";
        }

        private static readonly EnableAugmentedEnumerationExtensionTargetData.EqComp TheComparer =
            default(EnableAugmentedEnumerationExtensionTargetData).GetComparer();
    }
}