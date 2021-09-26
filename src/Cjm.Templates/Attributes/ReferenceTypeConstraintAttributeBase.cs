using System;

namespace Cjm.Templates.Attributes
{
    public abstract class ReferenceTypeConstraintAttributeBase : ConstraintAttribute, IEquatable<ReferenceTypeConstraintAttributeBase>
    {
        protected abstract ReferenceTypeConstraintVariant Constraint { get; }
        public bool AnyReferenceTypePasses => Constraint.AnyReferenceTypePasses;
        public bool ImplementorMustBeSealed => Constraint.ImplementorMustBeSealed;
        public bool ImplementorMustBeSealedOrAbstractAndNotExternallyInheritable =>
            Constraint.ImplementorMustBeSealedOrAbstractAndNotExternallyInheritable;
        public bool ImplementorMustBeImmutable => Constraint.ImplementorMustBeImmutable;
        public bool ImplementorMustBeDelegate => Constraint.ImplementorMustBeDelegate;
        public bool ImplementorMustBeSpecificDelegate => Constraint.ImplementorMustBeSpecificDelegate;

        public bool Equals(ReferenceTypeConstraintAttributeBase? other) => other?.Constraint == Constraint;

        /// <inheritdoc />
        protected sealed override string GetImplStringRep() => Constraint.ToString();
        /// <inheritdoc />
        protected sealed override bool IsEqualTo(ConstraintAttribute? other) =>
            Equals(other as ReferenceTypeConstraintAttributeBase);
        /// <inheritdoc />
        protected sealed override int CalculateHashCode() => Constraint.GetHashCode();
    }
}