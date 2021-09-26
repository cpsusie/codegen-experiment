using System;
using Cjm.Templates.Attributes;

namespace Cjm.Templates.ConstraintSpecifiers
{
    public sealed class BinaryComparisonOperatorSpecifier : BinaryOperatorSpecifier
    {
        public static BinaryComparisonOperatorSpecifier CreateEqualityOperatorSpecifier(Type delegateForm) =>
            new(delegateForm, OperatorSpecifier.CheckEquals);
        public static BinaryComparisonOperatorSpecifier CreateInequalityOperatorSpecifier(Type delegateForm) =>
            new(delegateForm, OperatorSpecifier.CheckNotEquals);
        public static BinaryComparisonOperatorSpecifier CreateGreaterThanOperatorSpecifier(Type delegateForm) =>
            new(delegateForm, OperatorSpecifier.GreaterThan);
        public static BinaryComparisonOperatorSpecifier CreateLessThanOperatorSpecifier(Type delegateForm) =>
            new(delegateForm, OperatorSpecifier.LessThan);
        public static BinaryComparisonOperatorSpecifier CreateGreaterThanOrEqualOperatorSpecifier(Type delegateForm) =>
            new(delegateForm, OperatorSpecifier.GreaterThanOrEqual);
        public static BinaryComparisonOperatorSpecifier CreateLessThanOperatorOrEqualSpecifier(Type delegateForm) =>
            new(delegateForm, OperatorSpecifier.LessThanOrEqualTo);

        private BinaryComparisonOperatorSpecifier(Type delegateForm, OperatorSpecifier specifier) : base(delegateForm, specifier)
        {
            if (specifier.Category != OperatorCategory.Relational && specifier.Category != OperatorCategory.Equality)
                throw new ArgumentException(
                    $"Parameter must be of the {nameof(OperatorCategory.Relational)} or " +
                    $"{nameof(OperatorCategory.Equality)} category.  Actual value: {specifier.Category}.",
                    nameof(specifier));
        }
    }
}