using System;
using Cjm.Templates.ConstraintSpecifiers;

namespace Cjm.Templates.Attributes
{
    [AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.GenericParameter, AllowMultiple = true)]
    public sealed class StaticMethodOperationConstraintAttribute : StaticOperationConstraintAttributeBase
    {
        public StaticMethodSpecifier MethodSpecifier => _methodSpecifier;

        /// <inheritdoc />
        protected override string GetImplStringRep() => $" Method specifier: [{_methodSpecifier}].";

        public StaticMethodOperationConstraintAttribute(StaticMethodSpecifier specifier) =>
            _methodSpecifier = specifier ?? throw new ArgumentNullException(nameof(specifier));

        public StaticMethodOperationConstraintAttribute(Type delegateForm, string methodName) => _methodSpecifier =
            StaticMethodSpecifier.CreateStaticMethodSpecifier(methodName, delegateForm ?? throw new ArgumentNullException(nameof(delegateForm)));
        

        /// <inheritdoc />
        protected override StaticOperationSpecifier OperationSpecifier => _methodSpecifier;

        private readonly StaticMethodSpecifier _methodSpecifier;
    }
}