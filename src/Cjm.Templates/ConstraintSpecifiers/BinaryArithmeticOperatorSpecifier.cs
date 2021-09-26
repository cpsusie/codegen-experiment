using System;
using Cjm.Templates.Attributes;

namespace Cjm.Templates.ConstraintSpecifiers
{
    public sealed class BinaryArithmeticOperatorSpecifier : BinaryOperatorSpecifier
    {
        public static BinaryArithmeticOperatorSpecifier CreateBinaryAdditionOperatorSpecifier(Type delegateForm) =>
            new(delegateForm, OperatorSpecifier.Addition); 
        public static BinaryArithmeticOperatorSpecifier CreateBinarySubractionOperatorSpecifier(Type delegateForm) =>
            new(delegateForm, OperatorSpecifier.Subtraction);
        public static BinaryArithmeticOperatorSpecifier CreateBinaryMultiplicationOperatorSpecifier(Type delegateForm) =>
            new(delegateForm, OperatorSpecifier.Multiplication);
        public static BinaryArithmeticOperatorSpecifier CreateBinaryDivisionOperatorSpecifier(Type delegateForm) =>
            new(delegateForm, OperatorSpecifier.Division);
        public static BinaryArithmeticOperatorSpecifier CreateBinaryModulusOperatorSpecifier(Type delegateForm) =>
            new(delegateForm, OperatorSpecifier.Modulus);

        private BinaryArithmeticOperatorSpecifier(Type delegateForm, OperatorSpecifier specifier) : base(delegateForm, specifier)
        {
            if (specifier.Category != OperatorCategory.Arithmetic)
                throw new ArgumentException(
                    $"Parameter must be of the {nameof(OperatorCategory.Arithmetic)} category.  Actual value: {specifier.Category}.",
                    nameof(specifier));
        }
    }
}