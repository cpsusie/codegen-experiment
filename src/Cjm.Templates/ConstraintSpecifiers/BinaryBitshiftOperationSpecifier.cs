using System;
using Cjm.Templates.Attributes;

namespace Cjm.Templates.ConstraintSpecifiers
{
    public sealed class BinaryBitshiftOperationSpecifier : BinaryOperatorSpecifier
    {
        public static BinaryBitshiftOperationSpecifier CreateLeftShiftOperatorSpecifier(Type delegateForm) =>
            new(delegateForm, OperatorSpecifier.LeftShift);
        public static BinaryBitshiftOperationSpecifier CreateRightShiftOperatorSpecifier(Type delegateForm) =>
            new(delegateForm, OperatorSpecifier.RightShift);

        /// <inheritdoc />
        private BinaryBitshiftOperationSpecifier(Type delegateForm, OperatorSpecifier specifier) : base(
            delegateForm, specifier)
        {
            if (specifier.Category != OperatorCategory.BitShift)
                throw new ArgumentException(
                    $"Parameter must be of the {nameof(OperatorCategory.Relational)} or" +
                    $" {nameof(OperatorCategory.Equality)} category.  Actual value: {specifier.Category}.",
                    nameof(specifier));
        }
    }
}