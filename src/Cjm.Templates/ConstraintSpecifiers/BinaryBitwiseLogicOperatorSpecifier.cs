using System;
using Cjm.Templates.Attributes;

namespace Cjm.Templates.ConstraintSpecifiers
{
    public sealed class BinaryBitwiseLogicOperatorSpecifier : BinaryOperatorSpecifier
    {
        public static BinaryBitwiseLogicOperatorSpecifier CreateBitwiseAndOperatorSpecifier(Type owningType,
            ParameterSpecifier returnType, ParameterSpecifier firstParam, ParameterSpecifier secondParameter) =>
            new(owningType, OperatorSpecifier.BitwiseAnd, returnType, firstParam, secondParameter);
        public static BinaryBitwiseLogicOperatorSpecifier CreateBitwiseOrOperatorSpecifier(Type owningType,
            ParameterSpecifier returnType, ParameterSpecifier firstParam, ParameterSpecifier secondParameter) =>
            new(owningType, OperatorSpecifier.BitwiseOr, returnType, firstParam, secondParameter);
        public static BinaryBitwiseLogicOperatorSpecifier CreateBitwiseXorOperatorSpecifier(Type owningType,
            ParameterSpecifier returnType, ParameterSpecifier firstParam, ParameterSpecifier secondParameter) =>
            new(owningType, OperatorSpecifier.BitwiseXor, returnType, firstParam, secondParameter);

        /// <inheritdoc />
        private BinaryBitwiseLogicOperatorSpecifier(Type owningType, OperatorSpecifier specifier,
            ParameterSpecifier returnType, ParameterSpecifier firstOperand, ParameterSpecifier secondOperand) : base(
            owningType, specifier, returnType, firstOperand, secondOperand)
        {
            if (specifier.Category != OperatorCategory.BitwiseLogic)
                throw new ArgumentException(
                    $"Parameter must be of the {nameof(OperatorCategory.Relational)} or" +
                    $" {nameof(OperatorCategory.Equality)} category.  Actual value: {specifier.Category}.",
                    nameof(specifier));
        }
    }
}