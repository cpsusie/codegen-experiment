using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using HpTimeStamps;
using LoggerLibrary;

namespace Cjm.Templates.Attributes
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct ReferenceTypeConstraintVariant : IEquatable<ReferenceTypeConstraintVariant>, IComparable<ReferenceTypeConstraintVariant>
    {

        public static implicit operator ReferenceTypeConstraintVariant(DelegateReferenceTypeConstraint drtc) =>
            new(drtc);
        public static implicit operator ReferenceTypeConstraintVariant(ReferenceTypeConstraint rtc) => new(rtc);
        public static explicit operator ReferenceTypeConstraint(in ReferenceTypeConstraintVariant variant) =>
            variant._delegateCode == RegularCode
                ? variant._t2
                : throw new InvalidOperationException("The variant does not store a ReferenceTypeConstraint.");
        public static explicit operator DelegateReferenceTypeConstraint(in ReferenceTypeConstraintVariant variant) =>
            variant._delegateCode == DelegateCode
                ? variant._t1
                : throw new InvalidOperationException("The variant does not store a DelegateReferenceTypeConstraint.");

        public bool AnyReferenceTypePasses => _delegateCode == RegularCode && _t2.AnyReferenceTypePasses;
        public bool ImplementorMustBeSealed => _delegateCode == RegularCode && _t2.ImplementorMustBeSealed;
        public bool ImplementorMustBeSealedOrAbstractAndNotExternallyInheritable =>
            _delegateCode == RegularCode && _t2.ImplementorMustBeSealedOrAbstractAndNotExternallyInheritable;
        public bool ImplementorMustBeImmutable => _delegateCode == RegularCode && _t2.ImplementorMustBeImmutable;
        public bool ImplementorMustBeDelegate => _delegateCode == DelegateCode;
        public bool ImplementorMustBeSpecificDelegate => _delegateCode == DelegateCode && !_t1.AnyDelegate;
        public bool IsEmptyOrInvalid => _delegateCode != DelegateCode && _delegateCode != RegularCode;
        public bool IsDelegateConstraint => _delegateCode == DelegateCode;
        public bool IsRefBaseConstraint => _delegateCode == RegularCode;

        public static int Compare(in ReferenceTypeConstraintVariant l, in ReferenceTypeConstraintVariant r)
        {
            int ret = l._delegateCode.CompareTo(r._delegateCode);
            if (ret == 0)
            {
                ret = (l._delegateCode) switch
                {
                    DelegateCode => DelegateReferenceTypeConstraint.Compare(l._t1, r._t1),
                    RegularCode => l._t2.CompareTo(r._t2),
                    _ => 0
                };
            }
            return ret;
        }

        public override int GetHashCode()
        {
            int hash = _delegateCode;
            unchecked
            {
                hash = (hash * 397) ^ ((_delegateCode) switch
                {
                    DelegateCode => _t1.GetHashCode(),
                    RegularCode => _t2.GetHashCode(),
                    _ => 0,
                });
            }
            return hash;
        }

        public static bool operator ==(in ReferenceTypeConstraintVariant l,
            in ReferenceTypeConstraintVariant r)
        {
            bool ret = false;
            if (l._delegateCode == r._delegateCode)
            {
                ret = (l._delegateCode) switch
                {
                    DelegateCode => l._t1 == r._t1,
                    RegularCode => l._t2 == r._t2,
                    _ => true,
                };
            }
            return ret;
        }
        public override bool Equals(object? other) => other switch
        {
            null => false,
            DelegateReferenceTypeConstraint drtc => drtc == this,
            ReferenceTypeConstraint rtc => rtc == this,
            ReferenceTypeConstraintVariant otherVariant => otherVariant == this,
            _ => false,
        };

        public static bool operator !=(in ReferenceTypeConstraintVariant l,
            in ReferenceTypeConstraintVariant r) => !(l == r);
        public static bool operator >(in ReferenceTypeConstraintVariant l, in ReferenceTypeConstraintVariant r) =>
            Compare(in l, in r) > 0;
        public static bool operator <(in ReferenceTypeConstraintVariant l, in ReferenceTypeConstraintVariant r) =>
            Compare(in l, in r) < 0;
        public static bool operator >=(in ReferenceTypeConstraintVariant l, in ReferenceTypeConstraintVariant r) =>
            !(l < r);
        public static bool operator <=(in ReferenceTypeConstraintVariant l, in ReferenceTypeConstraintVariant r) =>
            !(l > r);
        public bool Equals(ReferenceTypeConstraintVariant other) => other == this;
        public int CompareTo(ReferenceTypeConstraintVariant other) => Compare(in this, in other);

        /// <inheritdoc />
        public override string ToString() => (_delegateCode) switch
        {
            DelegateCode => $"Variant Type - [Delegate Constraint]; Contents: [{_t1.ToString()}].",
            RegularCode => $"Variant Type - [Ref Type Constraint]; Contents: [{_t2.ToString()}].",
            _ => $"Variant Type - [EMPTY VARIANT]",
        };
        
        private ReferenceTypeConstraintVariant(DelegateReferenceTypeConstraint drtc)
        {
            _delegateCode = DelegateCode;
            _t2 = default;
            _t1 = drtc;
        }

        private ReferenceTypeConstraintVariant(ReferenceTypeConstraint rtc)
        {
            _delegateCode = RegularCode;
            _t1 = default;
            _t2 = rtc;
        }


        private readonly int _delegateCode;
        private readonly DelegateReferenceTypeConstraint _t1;
        private readonly ReferenceTypeConstraint _t2;

        private const int EmptyCode = 0;
        private const int DelegateCode = 1;
        private const int RegularCode = 2;
    }

    [StructLayout(LayoutKind.Explicit)]
    public readonly struct DelegateReferenceTypeConstraint : IEquatable<DelegateReferenceTypeConstraint>,
        IComparable<DelegateReferenceTypeConstraint>
    {
        public static readonly DelegateReferenceTypeConstraint AnyDelegateConstraint = new(null);

        public static DelegateReferenceTypeConstraint
            CreateSpecificDelegateTypeConstraint(Type delegateType) => new(delegateType);

        public Type? MustBeAssignableToDelegateOfType => _mustBeAssignableToDelegateOfType;

        public bool AnyDelegate => MustBeAssignableToDelegateOfType == null;

        

        private DelegateReferenceTypeConstraint(Type? delegateType) =>
            _mustBeAssignableToDelegateOfType = (delegateType)
                switch
                {
                    null => null,
                    {} dt when dt == typeof(Delegate) => null,
                    {} dt when typeof(Delegate).IsAssignableFrom(dt) => dt,
                    _ => throw new ArgumentException("Parameter must be a delegate type or null."),
                };

        public static bool operator ==(DelegateReferenceTypeConstraint lhs, DelegateReferenceTypeConstraint rhs) =>
            lhs._mustBeAssignableToDelegateOfType == rhs._mustBeAssignableToDelegateOfType;
        public static bool operator !=(DelegateReferenceTypeConstraint lhs, DelegateReferenceTypeConstraint rhs) =>
            !(lhs == rhs);
        public override int GetHashCode() => _mustBeAssignableToDelegateOfType?.GetHashCode() ?? int.MinValue;
        public override bool Equals(object? other) => other is DelegateReferenceTypeConstraint drtc && drtc == this;
        public bool Equals(DelegateReferenceTypeConstraint other) => other == this;
        public int CompareTo(DelegateReferenceTypeConstraint other) => Compare(this, other);
        public static int Compare(DelegateReferenceTypeConstraint lhs,DelegateReferenceTypeConstraint rhs)
        {
            return CompareTypes(lhs._mustBeAssignableToDelegateOfType, rhs._mustBeAssignableToDelegateOfType);
            static int CompareTypes(Type? l, Type? r)
            {
                if (ReferenceEquals(l, r)) return 0;
                if (ReferenceEquals(l, null)) return -1;
                if (ReferenceEquals(r, null)) return 1;

                return TheTypeComparer.Compare(l.AssemblyQualifiedName ?? (l.FullName ?? l.Name),
                    r.AssemblyQualifiedName ?? (r.FullName ?? r.Name));
            }
        }



        [FieldOffset(0)] private readonly Type? _mustBeAssignableToDelegateOfType;
        private static readonly StringComparer TheTypeComparer = StringComparer.Ordinal;
    }


    [StructLayout(LayoutKind.Explicit)]
    public readonly struct ReferenceTypeConstraint 
        : IEquatable<ReferenceTypeConstraint>, IComparable<ReferenceTypeConstraint>
    {
        public static ReferenceTypeConstraint AnyReferenceTypeConstraint { get; } =
            new(ReferenceTypeImplementationConstraintCode.AnyReferenceType);
        public static ReferenceTypeConstraint MustBeSealedConstraint { get; } =
            new(ReferenceTypeImplementationConstraintCode.MustBeSealed);
        public static ReferenceTypeConstraint MustBeAbstractOrSealed { get; } =
            new(ReferenceTypeImplementationConstraintCode.MustBeAbstractOrSealed);
        public static ReferenceTypeConstraint MustBeImmutable { get; } =
            new(ReferenceTypeImplementationConstraintCode.MustBeImmutable);
        public static ReferenceTypeConstraint MustBeImmutableAndSealed { get; } =
            new(ReferenceTypeImplementationConstraintCode.MustBeImmutableAndSealed);
        

        public static implicit operator ReferenceTypeConstraint(
            ReferenceTypeImplementationConstraintCode code) => new(code);
        public static implicit operator ReferenceTypeImplementationConstraintCode(
            ReferenceTypeConstraint constr) => constr._code;

        public bool AnyReferenceTypePasses => _code == ReferenceTypeImplementationConstraintCode.AnyReferenceType;
        public bool ImplementorMustBeSealed => _code == ReferenceTypeImplementationConstraintCode.MustBeSealed;
        public bool ImplementorMustBeSealedOrAbstractAndNotExternallyInheritable =>
            _code is ReferenceTypeImplementationConstraintCode.MustBeAbstractOrSealed or ReferenceTypeImplementationConstraintCode.MustBeImmutable;
        public bool ImplementorMustBeImmutable => _code is ReferenceTypeImplementationConstraintCode.MustBeImmutable or ReferenceTypeImplementationConstraintCode
            .MustBeImmutableAndSealed;
        
        private ReferenceTypeConstraint(ReferenceTypeImplementationConstraintCode code) =>
            _code = code.ValueOrThrowIfNDef(nameof(code));
        
        public static bool operator ==(ReferenceTypeConstraint lhs,
            ReferenceTypeConstraint rhs) => lhs._code == rhs._code;
        public static bool operator !=(ReferenceTypeConstraint lhs,
            ReferenceTypeConstraint rhs) => !(lhs == rhs);
        public static bool operator >(ReferenceTypeConstraint lhs,
            ReferenceTypeConstraint rhs) => lhs._code > rhs._code;
        public static bool operator <(ReferenceTypeConstraint lhs,
            ReferenceTypeConstraint rhs) => lhs._code < rhs._code;
        public static bool operator >=(ReferenceTypeConstraint lhs,
            ReferenceTypeConstraint rhs) => lhs._code >= rhs._code;
        public static bool operator <=(ReferenceTypeConstraint lhs,
            ReferenceTypeConstraint rhs) => lhs._code <= rhs._code;
        public override int GetHashCode() => ((byte)_code);
        public override bool Equals(object? obj) => obj is ReferenceTypeConstraint rtc && rtc == this;
        public bool Equals(ReferenceTypeConstraint other) => other == this;
        public int CompareTo(ReferenceTypeConstraint other) => ((byte)_code).CompareTo((byte)other._code);
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