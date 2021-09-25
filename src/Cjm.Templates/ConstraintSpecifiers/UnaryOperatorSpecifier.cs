using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Cjm.Templates.Attributes;

namespace Cjm.Templates.ConstraintSpecifiers
{
    public abstract class UnaryOperatorSpecifier : StaticOperatorSpecifierBase
    {
        public sealed override ImmutableArray<ParameterSpecifier> InputParameterList => _inputParameters;
        public ParameterSpecifier InputParameter => InputParameterList[0];

        protected UnaryOperatorSpecifier(Type owningType, OperatorSpecifier specifier,
            ParameterSpecifier returnType, ParameterSpecifier inputOperand) : base(owningType, specifier, returnType)
        {
            if (specifier.Form != OperatorForm.Unary)
                throw new ArgumentException(
                    $"Specifier must be of form {nameof(OperatorForm.Unary)} but it's actual value is {specifier.Form}.",
                    nameof(specifier));
            _inputParameters = ImmutableArray.Create(
                ValueOrThrowIfVoid(in inputOperand, nameof(inputOperand)));
            Debug.Assert(_inputParameters.Length == 1);
        }

        protected override bool IsImplEqualTo(StaticOperationSpecifier? other) =>
            other is BinaryArithmeticOperatorSpecifier baos && baos.Specifier == Specifier;
        protected override int GetImplHashCode() => Specifier.GetHashCode();
        protected override string GetImplStringRep() => Specifier.ToString();

        private readonly ImmutableArray<ParameterSpecifier> _inputParameters;
    }
}