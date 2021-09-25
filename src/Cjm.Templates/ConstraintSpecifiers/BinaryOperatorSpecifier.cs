using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Cjm.Templates.Attributes;

namespace Cjm.Templates.ConstraintSpecifiers
{
    public abstract class BinaryOperatorSpecifier : StaticOperatorSpecifierBase
    {
        public sealed override ImmutableArray<ParameterSpecifier> InputParameterList => _inputParameters;
        public ParameterSpecifier FirstOperand => InputParameterList[0];
        public ParameterSpecifier SecondOperand => InputParameterList[1];

        protected BinaryOperatorSpecifier(Type owningType, OperatorSpecifier specifier,
            ParameterSpecifier returnType, ParameterSpecifier firstOperand, ParameterSpecifier secondOperand) : base(
            owningType ?? throw new ArgumentNullException(nameof(owningType)), specifier, returnType)
        {
            if (specifier.Form != OperatorForm.Binary)
                throw new ArgumentException(
                    $"Parameter must be of the {nameof(OperatorForm.Binary)} form.  Actual value: {specifier.Form}.",
                    nameof(specifier));
            _inputParameters = ImmutableArray.Create(
                ValueOrThrowIfVoid(in firstOperand, nameof(firstOperand)),
                ValueOrThrowIfVoid(in secondOperand, nameof(secondOperand)));
            Debug.Assert(_inputParameters.Length == 2);
        }
        protected override bool IsImplEqualTo(StaticOperationSpecifier? other) =>
            other is BinaryArithmeticOperatorSpecifier baos && baos.Specifier == Specifier;
        protected override int GetImplHashCode() => Specifier.GetHashCode();
        protected override string GetImplStringRep() => Specifier.ToString();
        private readonly ImmutableArray<ParameterSpecifier> _inputParameters;
    }
}