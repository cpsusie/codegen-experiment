using System;
using Cjm.Templates.Attributes;

namespace Cjm.Templates.ConstraintSpecifiers
{
    public abstract class StaticOperatorSpecifierBase : StaticOperationSpecifier
    {
        /// <inheritdoc />
        public sealed override Type OperationFormDelegate { get; }
        public sealed override string OperationName { get; }
        public OperatorSpecifier Specifier { get; }
        public sealed override bool IsMethod => false;

        /// <inheritdoc />
        protected override bool IsImplEqualTo(StaticOperationSpecifier? other) =>
            other is StaticOperatorSpecifierBase ob && ob.Specifier == Specifier;
        /// <inheritdoc />
        protected override int GetImplHashCode() => Specifier.GetHashCode();
        /// <inheritdoc />
        protected override string GetImplStringRep() => $"Operator: \t[" + Specifier.ToString() + "].";
        

        protected StaticOperatorSpecifierBase(Type operatorFormType, OperatorSpecifier specifier)
        {
            OperationFormDelegate = operatorFormType ?? throw new ArgumentNullException(nameof(operatorFormType));
            Specifier = specifier;
            OperationName = specifier.Name.ToString();
        }
    }
}