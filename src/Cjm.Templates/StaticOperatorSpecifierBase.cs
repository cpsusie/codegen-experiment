using System;
using Cjm.Templates.Attributes;

namespace Cjm.Templates
{
    public abstract class StaticOperatorSpecifierBase : StaticOperationSpecifier
    {
        public sealed override string OperationName { get; }

        public sealed override bool IsMethod => false;
        public OperatorSpecifier Specifier { get; }

        protected StaticOperatorSpecifierBase(Type owningType, OperatorSpecifier specifier,
            ParameterSpecifier returnVal) : base(owningType ?? throw new ArgumentNullException(nameof(owningType)), returnVal)
        {
            Specifier = specifier;
            OperationName = specifier.Name.ToString();
            if (returnVal.ParameterType == typeof(void))
                throw new ArgumentException($"{typeof(void).Name} is not a valid return type for an operator.");
        }
    }
}