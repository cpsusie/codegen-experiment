using System;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonotonicContext = HpTimeStamps.MonotonicStampContext;
#nullable enable
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

        public bool Equals(GeneratorTestingPayloadEventArgs? value) => !ReferenceEquals(value, null) &&
                                                                       (value.ConcreteType == ConcreteType &&
                                                                        IsEqualTo(value));

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
        GeneratorTestingRefTypePayloadEventArgs<TPayload> : GeneratorTestingPayloadEventArgs
        where TPayload : class
    {
        public TPayload Payload => _payload;
        protected sealed override Type PayloadType => typeof(TPayload);

        protected GeneratorTestingRefTypePayloadEventArgs(TPayload payload) =>
            _payload = payload ?? throw new ArgumentNullException(nameof(payload));

        protected sealed override string ExecuteGetStringRep() => $"payload type [{PayloadType.Name}] with value: [" + GetPayloadString() + "].";
        
        protected abstract string GetPayloadString();
        private readonly TPayload _payload;
    } 

    public sealed class GeneratorTestingEnableAugmentedEnumerationFinalPayloadEventArgs : 
        GeneratorTestingRefTypePayloadEventArgs<
            ImmutableSortedDictionary<ClassDeclarationSyntax, ImmutableArray<SemanticData>>>
    {
        /// <inheritdoc />
        public GeneratorTestingEnableAugmentedEnumerationFinalPayloadEventArgs(
            ImmutableSortedDictionary<ClassDeclarationSyntax, ImmutableArray<SemanticData>> payload) :
            base(payload)
        {
            _str = new LocklessLazyWriteOnce<string>(InitPayloadText);
        }

        /// <inheritdoc />
        protected override bool IsEqualTo(GeneratorTestingPayloadEventArgs other) =>
            other is GeneratorTestingEnableAugmentedEnumerationFinalPayloadEventArgs ftpEventArgs &&
            Payload.Equals(ftpEventArgs.Payload);

        /// <inheritdoc />
        protected override int ExecuteGetHashCode() => Payload.GetHashCode();

        /// <inheritdoc />
        protected override string GetPayloadString() => _str.Value;


        private string InitPayloadText()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Lookup with {Payload.Count} unique class declarations: ");
            int keyCount = 0;
            foreach (var kvp in Payload)
            {
                ImmutableArray<SemanticData> matches =
                    kvp.Value.IsDefault ? ImmutableArray<SemanticData>.Empty : kvp.Value;
                sb.AppendLine($" \tFor cds key #{++keyCount} ({kvp.Key}) there are {matches.Length} distinct semantic matches:");
                for (int i = 0; i < matches.Length; ++i)
                {
                    var match = matches[i];
                    sb.AppendLine($" \t\tMatch #{i + 1}: ");
                    sb.AppendLine($" \t\t\t {match}");
                }

                sb.AppendLine($" \tDone printing matches for key# {keyCount}.");
            }
            sb.AppendLine("Done Printing Payload");
            return sb.ToString();
        }

        private readonly LocklessLazyWriteOnce<string> _str;
    }

    public abstract class
        GeneratorTestingPayloadEventArgs<TPayload, TPayloadComparer> : GeneratorTestingPayloadEventArgs
        where TPayloadComparer : unmanaged, IByRoRefEqualityComparer<TPayload>
        where TPayload : struct, IEquatable<TPayload>, IHasGenericByRefRoEqComparer<TPayloadComparer, TPayload>
    {
        public ref readonly TPayload Payload => ref _payload;

        /// <inheritdoc />
        protected sealed override Type PayloadType => typeof(TPayload);

        protected GeneratorTestingPayloadEventArgs(in TPayload payload)
            => _payload = payload;
        /// <inheritdoc />
        protected sealed override string ExecuteGetStringRep() => $"payload type [{PayloadType.Name}] with value: [" + GetPayloadString() + "].";

        protected abstract string GetPayloadString();
        private readonly TPayload _payload;
    }

    public sealed class
        GeneratorTestEnableAugmentSemanticPayloadEventArgs : GeneratorTestingRefTypePayloadEventArgs<SemanticData>
    {
        public GeneratorTestEnableAugmentSemanticPayloadEventArgs(SemanticData payload) 
            : base(payload ?? throw new ArgumentNullException(nameof(payload))) {}

        protected override bool IsEqualTo(GeneratorTestingPayloadEventArgs other) =>
            other is GeneratorTestEnableAugmentSemanticPayloadEventArgs val &&
            val.Payload == Payload;

        /// <inheritdoc />
        protected override int ExecuteGetHashCode() => Payload.GetHashCode();       

        /// <inheritdoc />
        protected override string GetPayloadString() => Payload.ToString();
        
    }

    public sealed class GeneratorTestEnableAugmentSyntaxReceiverPayloadEventArgs : GeneratorTestingPayloadEventArgs<
        EnableAugmentedEnumerationExtensionTargetData, EnableAugmentedEnumerationExtensionTargetData.EqComp>
    {
        /// <inheritdoc />
        public GeneratorTestEnableAugmentSyntaxReceiverPayloadEventArgs(
            in EnableAugmentedEnumerationExtensionTargetData payload) 
            : base(in payload) {}

        
        /// <inheritdoc />
        protected override bool IsEqualTo(GeneratorTestingPayloadEventArgs other) =>
            other is GeneratorTestEnableAugmentSyntaxReceiverPayloadEventArgs a &&
            TheComparer.Equals(in Payload, in a.Payload);

        /// <inheritdoc />
        protected override int ExecuteGetHashCode() => TheComparer.GetHashCode(in Payload);


        private static readonly EnableAugmentedEnumerationExtensionTargetData.EqComp TheComparer =
            default(EnableAugmentedEnumerationExtensionTargetData).GetComparer();

        /// <inheritdoc />
        protected override string GetPayloadString() => Payload.ToString();

    }
}