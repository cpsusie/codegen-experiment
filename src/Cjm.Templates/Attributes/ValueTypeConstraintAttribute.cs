using System;

namespace Cjm.Templates.Attributes
{
    public sealed class ValueTypeConstraintAttribute : ConstraintAttribute, IEquatable<ValueTypeConstraintAttribute>
    {
        public ValueTypeConstraint Constraint { get; } 

        public ValueTypeConstraintAttribute() => Constraint =  ValueTypeConstraint.PlainValueTypeConstraint;

        public ValueTypeConstraintAttribute(EnumConstraintType ect) => Constraint = ect;

        public ValueTypeConstraintAttribute(ValueTypeConstraintCode structConstraintCode) =>
            Constraint = structConstraintCode;

        public bool Equals(ValueTypeConstraintAttribute? other) => other?.Constraint == Constraint;


        /// <inheritdoc />
        protected override string GetImplStringRep() => Constraint.ToString();


        /// <inheritdoc />
        protected override bool IsEqualTo(ConstraintAttribute? other) => Equals(other as ValueTypeConstraintAttribute);


        /// <inheritdoc />
        protected override int CalculateHashCode() => Constraint.GetHashCode();

    }
}