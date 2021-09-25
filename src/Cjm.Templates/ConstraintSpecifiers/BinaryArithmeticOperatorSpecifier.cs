using System;
using Cjm.Templates.Attributes;

namespace Cjm.Templates.ConstraintSpecifiers
{
    public sealed class BinaryArithmeticOperatorSpecifier : BinaryOperatorSpecifier
    {
        public static BinaryArithmeticOperatorSpecifier CreateBinaryAdditionOperatorSpecifier(Type owningType,
            ParameterSpecifier returnType, ParameterSpecifier firstOperand, ParameterSpecifier secondOperand) =>
            new(owningType, OperatorSpecifier.Addition, returnType, firstOperand, secondOperand);
        public static BinaryArithmeticOperatorSpecifier CreateBinarySubractionOperatorSpecifier(Type owningType,
            ParameterSpecifier returnType, ParameterSpecifier firstOperand, ParameterSpecifier secondOperand) =>
            new(owningType, OperatorSpecifier.Subtraction, returnType, firstOperand, secondOperand);
        public static BinaryArithmeticOperatorSpecifier CreateBinaryMultiplicationOperatorSpecifier(Type owningType,
            ParameterSpecifier returnType, ParameterSpecifier firstOperand, ParameterSpecifier secondOperand) =>
            new(owningType, OperatorSpecifier.Multiplication, returnType, firstOperand, secondOperand);
        public static BinaryArithmeticOperatorSpecifier CreateBinaryDivisionOperatorSpecifier(Type owningType,
            ParameterSpecifier returnType, ParameterSpecifier firstOperand, ParameterSpecifier secondOperand) =>
            new(owningType, OperatorSpecifier.Division, returnType, firstOperand, secondOperand);
        public static BinaryArithmeticOperatorSpecifier CreateBinaryModulusOperatorSpecifier(Type owningType,
            ParameterSpecifier returnType, ParameterSpecifier firstOperand, ParameterSpecifier secondOperand) =>
            new(owningType, OperatorSpecifier.Modulus, returnType, firstOperand, secondOperand);
        
        
        private BinaryArithmeticOperatorSpecifier(Type owningType, OperatorSpecifier specifier,
            ParameterSpecifier returnType, ParameterSpecifier firstOperand, ParameterSpecifier secondOperand) : base(
            owningType ?? throw new ArgumentNullException(nameof(owningType)), specifier, returnType, firstOperand, secondOperand)
        {
            if (specifier.Category != OperatorCategory.Arithmetic)
                throw new ArgumentException(
                    $"Parameter must be of the {nameof(OperatorCategory.Arithmetic)} category.  Actual value: {specifier.Category}.",
                    nameof(specifier));
        }
    }
}