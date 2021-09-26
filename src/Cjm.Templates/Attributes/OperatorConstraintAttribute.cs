using System;
using Cjm.Templates.ConstraintSpecifiers;
using HpTimeStamps;

namespace Cjm.Templates.Attributes
{
    [AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.GenericParameter, AllowMultiple = true)]
    public sealed class OperatorConstraintAttribute : StaticOperationConstraintAttributeBase
    {
        protected override StaticOperationSpecifier OperationSpecifier => SpecifierBase;
        public StaticOperatorSpecifierBase SpecifierBase { get; }
        public OperatorSpecifier OperatorSpecification => SpecifierBase.Specifier;

        public OperatorConstraintAttribute(Type formDelegate, OperatorName name)
        {
            if (formDelegate == null) throw new ArgumentNullException(nameof(formDelegate));
            SpecifierBase = name switch
            {
                OperatorName.UnaryPlus => UnaryBitwiseOrArithmeticOperatorSpecifier.CreateUnaryPlusOperatorSpecifier(
                    formDelegate),
                OperatorName.UnaryMinus => UnaryBitwiseOrArithmeticOperatorSpecifier.CreateUnaryMinusOperatorSpecifier(
                    formDelegate),
                OperatorName.Increment => UnaryBitwiseOrArithmeticOperatorSpecifier.CreateIncrementOperatorSpecifier(
                    formDelegate),
                OperatorName.Decrement => UnaryBitwiseOrArithmeticOperatorSpecifier.CreateDecrementOperatorSpecifier(
                    formDelegate),
                OperatorName.ExplicitConversion => ConversionOperatorSpecifier.CreateExplicitConversionSpecifier(
                    formDelegate),
                OperatorName.ImplicitConversion => ConversionOperatorSpecifier.CreateImplicitConversionSpecifier(
                    formDelegate),
                OperatorName.CheckEquals => BinaryComparisonOperatorSpecifier.CreateEqualityOperatorSpecifier(
                    formDelegate),
                OperatorName.CheckNotEquals => BinaryComparisonOperatorSpecifier.CreateInequalityOperatorSpecifier(
                    formDelegate),
                OperatorName.GreaterThan => BinaryComparisonOperatorSpecifier.CreateGreaterThanOperatorSpecifier(
                    formDelegate),
                OperatorName.LessThan =>
                    BinaryComparisonOperatorSpecifier.CreateLessThanOperatorSpecifier(formDelegate),
                OperatorName.GreaterThanOrEqual => BinaryComparisonOperatorSpecifier
                    .CreateGreaterThanOrEqualOperatorSpecifier(formDelegate),
                OperatorName.LessThanOrEqual =>
                    BinaryComparisonOperatorSpecifier.CreateLessThanOperatorOrEqualSpecifier(formDelegate),
                OperatorName.BitwiseAnd => BinaryBitwiseLogicOperatorSpecifier.CreateBitwiseAndOperatorSpecifier(
                    formDelegate),
                OperatorName.BitwiseOr => BinaryBitwiseLogicOperatorSpecifier.CreateBitwiseOrOperatorSpecifier(
                    formDelegate),
                OperatorName.BitwiseXor => BinaryBitwiseLogicOperatorSpecifier.CreateBitwiseXorOperatorSpecifier(
                    formDelegate),
                OperatorName.BitwiseNot => UnaryBitwiseOrArithmeticOperatorSpecifier.CreateBitwiseNotOperatorSpecifier(
                    formDelegate),
                OperatorName.LeftShift => BinaryBitshiftOperationSpecifier.CreateLeftShiftOperatorSpecifier(
                    formDelegate),
                OperatorName.RightShift => BinaryBitshiftOperationSpecifier.CreateRightShiftOperatorSpecifier(
                    formDelegate),
                OperatorName.Addition => BinaryArithmeticOperatorSpecifier.CreateBinaryAdditionOperatorSpecifier(
                    formDelegate),
                OperatorName.Subtraction => BinaryArithmeticOperatorSpecifier.CreateBinarySubractionOperatorSpecifier(
                    formDelegate),
                OperatorName.Multiplication => BinaryArithmeticOperatorSpecifier
                    .CreateBinaryMultiplicationOperatorSpecifier(formDelegate),
                OperatorName.Division => BinaryArithmeticOperatorSpecifier.CreateBinaryDivisionOperatorSpecifier(
                    formDelegate),
                OperatorName.Modulus => BinaryArithmeticOperatorSpecifier.CreateBinaryModulusOperatorSpecifier(
                    formDelegate),
                _ => throw new UndefinedEnumArgumentException<OperatorName>(name, nameof(name)),
            };
        }


        /// <inheritdoc />
        protected override string GetImplStringRep() => $"Operator: [{OperatorSpecification.Name}].";

    }
}