using System;

namespace Cjm.Templates.Attributes
{
    [AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.GenericParameter, AllowMultiple = false)]
    public sealed class DelegateReferenceTypeConstraintAttribute: ReferenceTypeConstraintAttributeBase
    {
        public static readonly DelegateReferenceTypeConstraintAttribute AnyDelegateConstraint = new(null, false);

        protected override ReferenceTypeConstraintVariant Constraint => _constraint;

        public Type? DelegateMustBeAssignableTo => _constraint.MustBeAssignableToDelegateOfType;

        public DelegateReferenceTypeConstraintAttribute(Type delegateType) => _constraint = delegateType switch
        {
            null => throw new ArgumentNullException(nameof(delegateType)),
            { } d when typeof(Delegate).IsAssignableFrom(d) => DelegateReferenceTypeConstraint
                .CreateSpecificDelegateTypeConstraint(d),
            _ => throw new ArgumentException(
                $"The parameter denote a type assignable to {nameof(Delegate)}. Actual: {delegateType.Name}.")
        };
        
        private DelegateReferenceTypeConstraintAttribute(Type? delegateType, bool _) => _constraint = delegateType == null
            ? DelegateReferenceTypeConstraint.AnyDelegateConstraint
            : DelegateReferenceTypeConstraint.CreateSpecificDelegateTypeConstraint(delegateType);

        private readonly DelegateReferenceTypeConstraint _constraint;
    }
}