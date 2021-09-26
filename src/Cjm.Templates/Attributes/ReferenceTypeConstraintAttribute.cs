using System;

namespace Cjm.Templates.Attributes
{
    [AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.GenericParameter, AllowMultiple = false)]
    public sealed class ReferenceTypeConstraintAttribute : ReferenceTypeConstraintAttributeBase
    {
        protected override ReferenceTypeConstraintVariant Constraint => _constraint;

        public ReferenceTypeConstraintAttribute(ReferenceTypeImplementationConstraintCode code) =>
            _constraint = code;

        private readonly ReferenceTypeConstraint _constraint;
    }
}