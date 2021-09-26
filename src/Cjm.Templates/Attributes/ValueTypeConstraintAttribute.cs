using System;
using Cjm.Templates.ConstraintSpecifiers;

namespace Cjm.Templates.Attributes
{
    [AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.GenericParameter)]
    public sealed class ValueTypeConstraintAttribute : ConstraintAttribute, IEquatable<ValueTypeConstraintAttribute>
    {
        public static implicit operator ValueTypeConstraintAttribute(ValueTypeConstraintSpecifier specifier) =>
            new(specifier);

        public static implicit operator ValueTypeConstraintAttribute(EnumConstraintType ect) => new(ect);
        public static implicit operator ValueTypeConstraintAttribute(ValueTypeConstraintCode code) => new(code);

        public ValueTypeConstraintSpecifier Constraint { get; } 

        public ValueTypeConstraintAttribute() => Constraint =  ValueTypeConstraintSpecifier.PlainValueTypeConstraint;

        public ValueTypeConstraintAttribute(EnumConstraintType ect) => Constraint = ect;

        public ValueTypeConstraintAttribute(ValueTypeConstraintCode structConstraintCode) =>
            Constraint = structConstraintCode;

        private ValueTypeConstraintAttribute(ValueTypeConstraintSpecifier specifier) => Constraint =specifier;

        public bool Equals(ValueTypeConstraintAttribute? other) => other?.Constraint == Constraint;


        /// <inheritdoc />
        protected override string GetImplStringRep() => Constraint.ToString();


        /// <inheritdoc />
        protected override bool IsEqualTo(ConstraintAttribute? other) => Equals(other as ValueTypeConstraintAttribute);


        /// <inheritdoc />
        protected override int CalculateHashCode() => Constraint.GetHashCode();

    }
}