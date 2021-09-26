using System;
using Cjm.Templates.Attributes;

namespace Cjm.Templates.ConstraintSpecifiers
{
    public sealed class UnaryBitwiseOrArithmeticOperatorSpecifier : UnaryOperatorSpecifier
    {
        public static UnaryBitwiseOrArithmeticOperatorSpecifier CreateUnaryPlusOperatorSpecifier(Type delegateForm) =>
            new(delegateForm, OperatorSpecifier.UnaryPlus);
        public static UnaryBitwiseOrArithmeticOperatorSpecifier CreateUnaryMinusOperatorSpecifier(Type delegateForm) =>
            new(delegateForm, OperatorSpecifier.UnaryMinus);
        public static UnaryBitwiseOrArithmeticOperatorSpecifier CreateIncrementOperatorSpecifier(Type delegateForm) =>
            new(delegateForm, OperatorSpecifier.Increment);
        public static UnaryBitwiseOrArithmeticOperatorSpecifier CreateDecrementOperatorSpecifier(Type delegateForm) =>
            new(delegateForm, OperatorSpecifier.Decrement);
        public static UnaryBitwiseOrArithmeticOperatorSpecifier CreateBitwiseNotOperatorSpecifier(Type delegateForm) => 
            new(delegateForm, OperatorSpecifier.BitwiseNot);


        private UnaryBitwiseOrArithmeticOperatorSpecifier(Type delegateForm, OperatorSpecifier specifier) : base(delegateForm, specifier)
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