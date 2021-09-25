using System;
using Cjm.Templates.Attributes;

namespace Cjm.Templates.ConstraintSpecifiers
{
    public sealed class ConversionOperatorSpecifier : UnaryOperatorSpecifier
    {
        public static ConversionOperatorSpecifier CreateImplicitConversionSpecifier(Type owningType,
            ParameterSpecifier returnType, ParameterSpecifier sourceType) => new(owningType,
            OperatorSpecifier.ImplicitConversion, returnType, sourceType);

        public static ConversionOperatorSpecifier CreateExplicitConversionSpecifier(Type owningType,
            ParameterSpecifier returnType, ParameterSpecifier sourceType) => new(owningType,
            OperatorSpecifier.ImplicitConversion, returnType, sourceType);


        public ParameterSpecifier ConvertFrom => InputParameterList[0];
        public bool IsExplicitConversion => Specifier.Name == OperatorName.ExplicitConversion;
        public bool IsImplicitConversion => !IsExplicitConversion;

        private ConversionOperatorSpecifier(Type owningType, OperatorSpecifier specifier, ParameterSpecifier returnType,
            ParameterSpecifier inputType) : base(owningType, specifier, returnType, inputType)
        {
            if (specifier.Category != OperatorCategory.Casting)
                throw new ArgumentException(
                    $"Parameter must be of the {nameof(OperatorCategory.Casting)} category.  Actual value: {specifier.Category}.",
                    nameof(specifier));
        }
    }
}