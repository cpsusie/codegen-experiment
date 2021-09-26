using System;
using Cjm.Templates.ConstraintSpecifiers;

namespace Cjm.Templates.Attributes
{
    [AttributeUsage(validOn: AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.GenericParameter, Inherited = true, AllowMultiple = false)]
    public sealed class FundamentalTypeConstraintVariantAttribute : Attribute, IEquatable<FundamentalTypeConstraintVariantAttribute>
    {
        #region Conversion Operators
        public static explicit operator
            DelegateReferenceTypeConstraintAttribute(FundamentalTypeConstraintVariantAttribute attrib) =>
            (attrib ?? throw new ArgumentNullException(nameof(attrib)))._wrappedConstraint as
            DelegateReferenceTypeConstraintAttribute ??
            throw new InvalidCastException(
                $"Type {attrib._wrappedConstraintType.Name} cannot be assigned to an object of type {nameof(DelegateReferenceTypeConstraintAttribute)}");
        public static explicit operator
            ReferenceTypeConstraintAttribute(FundamentalTypeConstraintVariantAttribute attrib) =>
            (attrib ?? throw new ArgumentNullException(nameof(attrib)))._wrappedConstraint as ReferenceTypeConstraintAttribute ??
            throw new InvalidCastException(
                $"Type {attrib._wrappedConstraintType.Name} cannot be assigned to an object of type {nameof(ReferenceTypeConstraintAttribute)}");
        public static explicit operator ValueTypeConstraintAttribute(FundamentalTypeConstraintVariantAttribute attrib) =>
            (attrib ?? throw new ArgumentNullException(nameof(attrib)))._wrappedConstraint as ValueTypeConstraintAttribute ??
            throw new InvalidCastException(
                $"Type {attrib._wrappedConstraintType.Name} cannot be assigned to an object of type {nameof(ValueTypeConstraintAttribute)}");
        public static implicit operator FundamentalTypeConstraintVariantAttribute(
            ReferenceTypeConstraintAttributeBase attribute) => attribute switch
        {
            null => throw new ArgumentNullException(nameof(attribute)),
            DelegateReferenceTypeConstraintAttribute delAttrib => new(delAttrib),
            ReferenceTypeConstraintAttribute rtAttrib => new(rtAttrib),
            _ => throw new ArgumentException(
                $"Requires a type of {nameof(ReferenceTypeConstraintAttribute)} or {nameof(DelegateReferenceTypeConstraintAttribute)}.  Actual type of parameter: {attribute.ConcreteConstraintType.Name}."),
        };
        public static implicit operator FundamentalTypeConstraintVariantAttribute(
            ValueTypeConstraintAttribute attribute) => attribute switch
        {
            null => throw new ArgumentNullException(nameof(attribute)),
            { } vAttrib => new(vAttrib),
        }; 
        #endregion

        #region Public Properties
        public Type VariantType => _wrappedConstraintType;

        public bool IsReferenceTypeConstraint => _wrappedConstraint is ReferenceTypeConstraintAttribute;

        public bool IsDelegateReferenceTypeConstraint => _wrappedConstraint is DelegateReferenceTypeConstraintAttribute;

        public bool IsValueTypeConstraint => _wrappedConstraint is ValueTypeConstraintAttribute; 
        #endregion

        #region Public CTORS
        public FundamentalTypeConstraintVariantAttribute(EnumConstraintType ect) : this((ValueTypeConstraintAttribute)ect) { }

        public FundamentalTypeConstraintVariantAttribute(ValueTypeConstraintCode ect) : this((ValueTypeConstraintAttribute)ect) { }

        public FundamentalTypeConstraintVariantAttribute(Type delegateType) : this(new DelegateReferenceTypeConstraintAttribute(delegateType)) { } 
        #endregion

        #region Private CTORs
        private FundamentalTypeConstraintVariantAttribute(DelegateReferenceTypeConstraintAttribute drtcAttrib) =>
            (_wrappedConstraintType, _wrappedConstraint) = drtcAttrib switch
            {
                { ConcreteConstraintType: { } cct } obj when typeof(DelegateReferenceTypeConstraintAttribute).IsAssignableFrom(cct)
                    => (cct, obj),
                null => throw new ArgumentNullException(nameof(drtcAttrib)),
                _ => throw new ArgumentException(
                    $"Object's concrete type must be assignable to {nameof(DelegateReferenceTypeConstraintAttribute)}.  Its actual type is {drtcAttrib.ConcreteConstraintType.Name}.")
            };
        private FundamentalTypeConstraintVariantAttribute(ValueTypeConstraintAttribute vtcAttrib) =>
            (_wrappedConstraintType, _wrappedConstraint) = vtcAttrib switch
            {
                { ConcreteConstraintType: { } cct } obj when typeof(ValueTypeConstraintAttribute).IsAssignableFrom(cct)
                    => (cct, obj),
                null => throw new ArgumentNullException(nameof(vtcAttrib)),
                _ => throw new ArgumentException(
                    $"Object's concrete type must be assignable to {nameof(ValueTypeConstraintAttribute)}.  Its actual type is {vtcAttrib.ConcreteConstraintType.Name}.")
            };

        private FundamentalTypeConstraintVariantAttribute(ReferenceTypeConstraintAttribute rtcAttribute) => (_wrappedConstraintType, _wrappedConstraint) = rtcAttribute switch
        {
            { ConcreteConstraintType: { } cct } obj when typeof(ReferenceTypeConstraintAttribute).IsAssignableFrom(cct)
                => (cct, obj),
            null => throw new ArgumentNullException(nameof(rtcAttribute)),
            _ => throw new ArgumentException(
                $"Object's concrete type must be assignable to {nameof(ReferenceTypeConstraintAttribute)}.  Its actual type is {rtcAttribute.ConcreteConstraintType.Name}.")
        };
        #endregion

        #region Public Methods and Operators
        public bool Equals(FundamentalTypeConstraintVariantAttribute? other) =>
            _wrappedConstraint == other?._wrappedConstraint && _wrappedConstraintType == other?._wrappedConstraintType;

        public override int GetHashCode()
        {
            int hash = _wrappedConstraintType.GetHashCode();
            unchecked
            {
                hash = (hash * 397) ^ _wrappedConstraint.GetHashCode();
            }
            return hash;
        }

        public override bool Equals(object? other) => other switch
        {
            DelegateReferenceTypeConstraintAttribute delConstr => delConstr == _wrappedConstraint,
            ReferenceTypeConstraintAttribute rtAttrib => rtAttrib == _wrappedConstraint,
            ValueTypeConstraintAttribute vtAttrib => vtAttrib == _wrappedConstraint,
            FundamentalTypeConstraintVariantAttribute otherVariant => Equals(otherVariant),
            _ => false
        };

        /// <inheritdoc />
        public override string ToString() =>
            $"[{nameof(FundamentalTypeConstraintVariantAttribute)}] -- Type: \t[{_wrappedConstraintType.Name}]; \tValue: \t[{_wrappedConstraint.ToString()}]";
        public static bool operator ==(FundamentalTypeConstraintVariantAttribute? lhs,
            FundamentalTypeConstraintVariantAttribute? rhs) => ReferenceEquals(lhs, rhs) || lhs?.Equals(rhs) == true;
        public static bool operator !=(FundamentalTypeConstraintVariantAttribute? lhs,
            FundamentalTypeConstraintVariantAttribute? rhs) => !(lhs == rhs);
        #endregion

        #region Private Data
        private readonly Type _wrappedConstraintType;
        private readonly ConstraintAttribute _wrappedConstraint; 
        #endregion
    }
}