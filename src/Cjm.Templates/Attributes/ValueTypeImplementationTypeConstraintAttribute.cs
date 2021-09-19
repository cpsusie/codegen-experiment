using System;

namespace Cjm.Templates.Attributes
{
    [AttributeUsage(AttributeTargets.Interface)]
    public sealed class ValueTypeImplementationTypeConstraintAttribute : InterfaceImplementationConstraintAttribute, IEquatable<ValueTypeImplementationTypeConstraintAttribute>
    {
        public ValueTypeConstraint Constraint { get; } 

        public ValueTypeImplementationTypeConstraintAttribute() => Constraint =  ValueTypeConstraint.PlainValueTypeConstraint;

        public ValueTypeImplementationTypeConstraintAttribute(EnumConstraintType ect) => Constraint = ect;

        public ValueTypeImplementationTypeConstraintAttribute(ValueTypeConstraintCode structConstraintCode) =>
            Constraint = structConstraintCode;

        public bool Equals(ValueTypeImplementationTypeConstraintAttribute? other) => other?.Constraint == Constraint;


        /// <inheritdoc />
        protected override string GetImplStringRep() => Constraint.ToString();


        /// <inheritdoc />
        protected override bool IsEqualTo(ConstraintAttribute? other) => Equals(other as ValueTypeImplementationTypeConstraintAttribute);


        /// <inheritdoc />
        protected override int CalculateHashCode() => Constraint.GetHashCode();

    }
}