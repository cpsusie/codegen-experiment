using System;
using Cjm.Templates.Attributes;

namespace Cjm.Templates.ConstraintSpecifiers
{
    public sealed class ConversionOperatorSpecifier : UnaryOperatorSpecifier
    {
        public static ConversionOperatorSpecifier CreateImplicitConversionSpecifier(Type delegateForm) => new(delegateForm, OperatorSpecifier.ImplicitConversion);
        public static ConversionOperatorSpecifier CreateExplicitConversionSpecifier(Type delegateForm) => new(delegateForm, OperatorSpecifier.ExplicitConversion);
        
        public bool IsExplicitConversion => Specifier.Name == OperatorName.ExplicitConversion;
        public bool IsImplicitConversion => !IsExplicitConversion;

        private ConversionOperatorSpecifier(Type delegateForm, OperatorSpecifier specifier) : base(delegateForm, specifier)
        {
            if (specifier.Category != OperatorCategory.Casting)
                throw new ArgumentException(
                    $"Parameter must be of the {nameof(OperatorCategory.Casting)} category.  Actual value: {specifier.Category}.",
                    nameof(specifier));
        }
    }
}