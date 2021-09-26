using System;
using Cjm.Templates.Attributes;

namespace Cjm.Templates.ConstraintSpecifiers
{
    public sealed class BinaryBitwiseLogicOperatorSpecifier : BinaryOperatorSpecifier
    {
        public static BinaryBitwiseLogicOperatorSpecifier CreateBitwiseAndOperatorSpecifier(Type delegateForm) =>
            new(delegateForm, OperatorSpecifier.BitwiseAnd);
        public static BinaryBitwiseLogicOperatorSpecifier CreateBitwiseOrOperatorSpecifier(Type delegateForm) =>
            new(delegateForm, OperatorSpecifier.BitwiseOr);
        public static BinaryBitwiseLogicOperatorSpecifier CreateBitwiseXorOperatorSpecifier(Type delegateForm) =>
            new(delegateForm, OperatorSpecifier.BitwiseXor);

        /// <inheritdoc />
        private BinaryBitwiseLogicOperatorSpecifier(Type delegateForm, OperatorSpecifier specifier) : base(delegateForm, specifier)
        {
            if (specifier.Category != OperatorCategory.BitwiseLogic)
                throw new ArgumentException(
                    $"Parameter must be of the {nameof(OperatorCategory.Relational)} or" +
                    $" {nameof(OperatorCategory.Equality)} category.  Actual value: {specifier.Category}.",
                    nameof(specifier));
        }
    }
}