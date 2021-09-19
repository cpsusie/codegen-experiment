using System;

namespace Cjm.Templates.Attributes
{
    [AttributeUsage(validOn: AttributeTargets.Interface | AttributeTargets.Class)]
    public sealed class ReferenceTypeImplementationConstraintAttribute : ConstraintAttribute, IEquatable<ReferenceTypeImplementationConstraintAttribute>
    {
        public ReferenceTypeImplementationConstraint Constraint { get; }
        public bool AnyReferenceTypePasses => Constraint.AnyReferenceTypePasses;
        public bool ImplementorMustBeSealed => Constraint.ImplementorMustBeSealed;
        public bool ImplementorMustBeSealedOrAbstractAndNotExternallyInheritable =>
            Constraint.ImplementorMustBeSealedOrAbstractAndNotExternallyInheritable;
        public bool ImplementorMustBeImmutable => Constraint.ImplementorMustBeImmutable;

        public ReferenceTypeImplementationConstraintAttribute() =>
            Constraint = ReferenceTypeImplementationConstraint.AnyReferenceTypeConstraint;
        public ReferenceTypeImplementationConstraintAttribute(ReferenceTypeImplementationConstraintCode code) =>
            Constraint = code;
        
        public bool Equals(ReferenceTypeImplementationConstraintAttribute? other) => other?.Constraint == Constraint;

        /// <inheritdoc />
        protected override string GetImplStringRep() => Constraint.ToString();
        /// <inheritdoc />
        protected override bool IsEqualTo(ConstraintAttribute? other) =>
            Equals(other as ReferenceTypeImplementationConstraintAttribute);
        /// <inheritdoc />
        protected override int CalculateHashCode() => Constraint.GetHashCode();

    }
}