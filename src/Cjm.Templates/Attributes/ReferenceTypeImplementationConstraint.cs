using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using HpTimeStamps;

namespace Cjm.Templates.Attributes
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct ReferenceTypeImplementationConstraint : IEquatable<ReferenceTypeImplementationConstraint>, IComparable<ReferenceTypeImplementationConstraint>
    {
        public static ReferenceTypeImplementationConstraint AnyReferenceTypeConstraint { get; } =
            new(ReferenceTypeImplementationConstraintCode.AnyReferenceType);
        public static ReferenceTypeImplementationConstraint MustBeSealedConstraint { get; } =
            new(ReferenceTypeImplementationConstraintCode.MustBeSealed);

        public static ReferenceTypeImplementationConstraint MustBeAbstractOrSealed { get; } =
            new(ReferenceTypeImplementationConstraintCode.MustBeAbstractOrSealed);
        public static ReferenceTypeImplementationConstraint MustBeImmutable { get; } =
            new(ReferenceTypeImplementationConstraintCode.MustBeImmutable);
        public static ReferenceTypeImplementationConstraint MustBeImmutableAndSealed { get; } =
            new(ReferenceTypeImplementationConstraintCode.MustBeImmutableAndSealed);
        

        public static implicit operator ReferenceTypeImplementationConstraint(
            ReferenceTypeImplementationConstraintCode code) => new(code);
        public static implicit operator ReferenceTypeImplementationConstraintCode(
            ReferenceTypeImplementationConstraint constr) => constr._code;

        public bool AnyReferenceTypePasses => _code == ReferenceTypeImplementationConstraintCode.AnyReferenceType;
        public bool ImplementorMustBeSealed => _code == ReferenceTypeImplementationConstraintCode.MustBeSealed;
        public bool ImplementorMustBeSealedOrAbstractAndNotExternallyInheritable =>
            _code is ReferenceTypeImplementationConstraintCode.MustBeAbstractOrSealed or ReferenceTypeImplementationConstraintCode.MustBeImmutable;
        public bool ImplementorMustBeImmutable => _code is ReferenceTypeImplementationConstraintCode.MustBeImmutable or ReferenceTypeImplementationConstraintCode
            .MustBeImmutableAndSealed;
        
        private ReferenceTypeImplementationConstraint(ReferenceTypeImplementationConstraintCode code) =>
            _code = code.ValueOrThrowIfNDef(nameof(code));
        
        public static bool operator ==(ReferenceTypeImplementationConstraint lhs,
            ReferenceTypeImplementationConstraint rhs) => lhs._code == rhs._code;
        public static bool operator !=(ReferenceTypeImplementationConstraint lhs,
            ReferenceTypeImplementationConstraint rhs) => !(lhs == rhs);
        public static bool operator >(ReferenceTypeImplementationConstraint lhs,
            ReferenceTypeImplementationConstraint rhs) => lhs._code > rhs._code;
        public static bool operator <(ReferenceTypeImplementationConstraint lhs,
            ReferenceTypeImplementationConstraint rhs) => lhs._code < rhs._code;
        public static bool operator >=(ReferenceTypeImplementationConstraint lhs,
            ReferenceTypeImplementationConstraint rhs) => lhs._code >= rhs._code;
        public static bool operator <=(ReferenceTypeImplementationConstraint lhs,
            ReferenceTypeImplementationConstraint rhs) => lhs._code <= rhs._code;
        public override int GetHashCode() => ((byte)_code);
        public override bool Equals(object? obj) => obj is ReferenceTypeImplementationConstraint rtc && rtc == this;
        public bool Equals(ReferenceTypeImplementationConstraint other) => other == this;
        public int CompareTo(ReferenceTypeImplementationConstraint other) => ((byte)_code).CompareTo((byte)other._code);
        /// <inheritdoc />
        public override string ToString() => _code.ToString();
        
        [FieldOffset(0)] private readonly ReferenceTypeImplementationConstraintCode _code;
    }

    public enum ReferenceTypeImplementationConstraintCode : byte
    {
        AnyReferenceType = 0,
        MustBeSealed,
        MustBeAbstractOrSealed,
        MustBeImmutable,
        MustBeImmutableAndSealed
    }

    public static class ReferenceTypeImplementationConstraintCodeExtensions
    {
        public static ImmutableArray<ReferenceTypeImplementationConstraintCode> DefinedValues =
            Enum.GetValues(typeof(ReferenceTypeImplementationConstraintCode))
                .Cast<ReferenceTypeImplementationConstraintCode>().ToImmutableArray();

        public static bool IsDefined(this ReferenceTypeImplementationConstraintCode code) =>
            DefinedValues.Contains(code);

        public static ReferenceTypeImplementationConstraintCode
            ValueOrThrowIfNDef(this ReferenceTypeImplementationConstraintCode code, string paramName) =>
            DefinedValues.Contains(code)
                ? code
                : throw new UndefinedEnumArgumentException<ReferenceTypeImplementationConstraintCode>(code,
                    paramName ?? throw new ArgumentNullException(nameof(paramName)));

    }
}