using System;
using System.Runtime.InteropServices;

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
}