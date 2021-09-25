using System;
using Cjm.Templates.Attributes;

namespace Cjm.Templates
{
    public sealed class UnaryBitwiseOrArithmeticOperatorSpecifier : UnaryOperatorSpecifier
    {
        public static UnaryBitwiseOrArithmeticOperatorSpecifier CreateUnaryPlusOperatorSpecifier(Type owningType,
            ParameterSpecifier returnType, ParameterSpecifier inputType) =>
            new(owningType, OperatorSpecifier.UnaryPlus, returnType, inputType);

        public static UnaryBitwiseOrArithmeticOperatorSpecifier CreateUnaryMinusOperatorSpecifier(Type owningType,
            ParameterSpecifier returnType, ParameterSpecifier inputType) =>
            new(owningType, OperatorSpecifier.UnaryMinus, returnType, inputType);

        public static UnaryBitwiseOrArithmeticOperatorSpecifier CreateIncrementOperatorSpecifier(Type owningType,
            ParameterSpecifier returnType, ParameterSpecifier inputType) =>
            new(owningType, OperatorSpecifier.Increment, returnType, inputType);

        public static UnaryBitwiseOrArithmeticOperatorSpecifier CreateDecrementOperatorSpecifier(Type owningType,
            ParameterSpecifier returnType, ParameterSpecifier inputType) =>
            new(owningType, OperatorSpecifier.Decrement, returnType, inputType);

        public static UnaryBitwiseOrArithmeticOperatorSpecifier CreateBitwiseNotOperatorSpecifier(Type owningType,
            ParameterSpecifier returnType,
            ParameterSpecifier inputType) => new(owningType, OperatorSpecifier.BitwiseNot, returnType, inputType);


        private UnaryBitwiseOrArithmeticOperatorSpecifier(Type owningType, OperatorSpecifier specifier,
            ParameterSpecifier returnType, ParameterSpecifier inputOperand) : base(owningType, specifier, returnType, inputOperand)
        {
            if (specifier.Category != OperatorCategory.IncDec && specifier.Category != OperatorCategory.BitwiseLogic &&
                specifier.Category != OperatorCategory.Arithmetic)
            {
                throw new ArgumentException(
                    $"Parameter must be of the {nameof(OperatorCategory.IncDec)}, {nameof(OperatorCategory.BitwiseLogic)}" +
                    $" or {nameof(OperatorCategory.Arithmetic)} category.  Actual value: {specifier.Category}.",
                    nameof(specifier));
            }
        }
    }
}