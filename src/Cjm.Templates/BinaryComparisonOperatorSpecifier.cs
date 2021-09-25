using System;
using Cjm.Templates.Attributes;

namespace Cjm.Templates
{
    public sealed class BinaryComparisonOperatorSpecifier : BinaryOperatorSpecifier
    {
        public static BinaryComparisonOperatorSpecifier CreateEqualityOperatorSpecifier(Type owningType,
            ParameterSpecifier firstComparand, ParameterSpecifier secondComparand) =>
            new(owningType, OperatorSpecifier.CheckEquals, firstComparand, secondComparand);
        public static BinaryComparisonOperatorSpecifier CreateInequalityOperatorSpecifier(Type owningType,
            ParameterSpecifier firstComparand, ParameterSpecifier secondComparand) =>
            new(owningType, OperatorSpecifier.CheckNotEquals, firstComparand, secondComparand);
        public static BinaryComparisonOperatorSpecifier CreateGreaterThanOperatorSpecifier(Type owningType,
            ParameterSpecifier firstComparand, ParameterSpecifier secondComparand) =>
            new(owningType, OperatorSpecifier.GreaterThan, firstComparand, secondComparand);
        public static BinaryComparisonOperatorSpecifier CreateLessThanOperatorSpecifier(Type owningType,
            ParameterSpecifier firstComparand, ParameterSpecifier secondComparand) =>
            new(owningType, OperatorSpecifier.LessThan, firstComparand, secondComparand);
        public static BinaryComparisonOperatorSpecifier CreateGreaterThanOrEqualOperatorSpecifier(Type owningType,
            ParameterSpecifier firstComparand, ParameterSpecifier secondComparand) =>
            new(owningType, OperatorSpecifier.GreaterThanOrEqual, firstComparand, secondComparand);
        public static BinaryComparisonOperatorSpecifier CreateLessThanOperatorOrEqualSpecifier(Type owningType,
            ParameterSpecifier firstComparand, ParameterSpecifier secondComparand) =>
            new(owningType, OperatorSpecifier.LessThanOrEqualTo, firstComparand, secondComparand);

        private BinaryComparisonOperatorSpecifier(Type owningType, OperatorSpecifier specifier,
            ParameterSpecifier firstOperand, ParameterSpecifier secondOperand) : base(
            owningType ?? throw new ArgumentNullException(nameof(owningType)), specifier,
            ParameterSpecifier.CreateParameterSpecifier(typeof(bool), NullabilitySpecifier.NotNull,
                PassBySpecifier.ByValue), firstOperand, secondOperand)
        {
            if (specifier.Category != OperatorCategory.Relational && specifier.Category != OperatorCategory.Equality)
                throw new ArgumentException(
                    $"Parameter must be of the {nameof(OperatorCategory.Relational)} or " +
                    $"{nameof(OperatorCategory.Equality)} category.  Actual value: {specifier.Category}.",
                    nameof(specifier));
        }
    }
}