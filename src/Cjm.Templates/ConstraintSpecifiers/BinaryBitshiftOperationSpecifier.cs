using System;
using Cjm.Templates.Attributes;

namespace Cjm.Templates.ConstraintSpecifiers
{
    public sealed class BinaryBitshiftOperationSpecifier : BinaryOperatorSpecifier
    {
        public static BinaryBitshiftOperationSpecifier CreateLeftShiftOperatorSpecifier(Type owningType,
            ParameterSpecifier returnType, ParameterSpecifier firstParam, ParameterSpecifier secondParameter) =>
            new(owningType, OperatorSpecifier.LeftShift, returnType, firstParam, secondParameter);
        public static BinaryBitshiftOperationSpecifier CreateRightShiftOperatorSpecifier(Type owningType,
            ParameterSpecifier returnType, ParameterSpecifier firstParam, ParameterSpecifier secondParameter) =>
            new(owningType, OperatorSpecifier.LeftShift, returnType, firstParam, secondParameter);

        /// <inheritdoc />
        private BinaryBitshiftOperationSpecifier(Type owningType, OperatorSpecifier specifier,
            ParameterSpecifier returnType, ParameterSpecifier firstOperand, ParameterSpecifier secondOperand) : base(
            owningType, specifier, returnType, firstOperand, secondOperand)
        {
            if (specifier.Category != OperatorCategory.BitShift)
                throw new ArgumentException(
                    $"Parameter must be of the {nameof(OperatorCategory.Relational)} or" +
                    $" {nameof(OperatorCategory.Equality)} category.  Actual value: {specifier.Category}.",
                    nameof(specifier));
        }
    }
}