using System;
using Cjm.Templates.ConstraintSpecifiers;

namespace Cjm.Templates.Attributes
{
    public sealed class ValueTypeConstraintAttribute : ConstraintAttribute, IEquatable<ValueTypeConstraintAttribute>
    {
        public ValueTypeConstraintSpecifier Constraint { get; } 

        public ValueTypeConstraintAttribute() => Constraint =  ValueTypeConstraintSpecifier.PlainValueTypeConstraint;

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