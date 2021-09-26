using Cjm.Templates.ConstraintSpecifiers;

namespace Cjm.Templates.Attributes
{
    public abstract class StaticOperationConstraintAttributeBase : ConstraintAttribute
    {
        protected abstract StaticOperationSpecifier OperationSpecifier { get; }

        /// <inheritdoc />
        protected sealed override bool IsEqualTo(ConstraintAttribute? other) =>
            other is StaticOperationConstraintAttributeBase socab && socab.OperationSpecifier == OperationSpecifier;

        /// <inheritdoc />
        protected sealed override int CalculateHashCode() => OperationSpecifier.GetHashCode();

    }
}