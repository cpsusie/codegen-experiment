using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using LoggerLibrary;

namespace Cjm.Templates
{
    public sealed class StaticMethodSpecifier : StaticOperationSpecifier
    {
        public static StaticMethodSpecifier CreateStaticMethodSpecifier(Type owningTypeName, string methodName,
            ParameterSpecifier returnTypeSpecifier, params ParameterSpecifier[] parameters)
        {
            if (owningTypeName == null) throw new ArgumentNullException(nameof(owningTypeName));
            string actualMethodName = (methodName ?? throw new ArgumentNullException(nameof(methodName))).Trim() switch
            {
                { } txt when string.IsNullOrWhiteSpace(txt) => throw new ArgumentException(
                    "Parameter may not be empty or whitespace-only.", nameof(methodName)),
                { } mn => mn
            };
            
            ImmutableArray<ParameterSpecifier> inputParams = (parameters) switch
            {
                null => ImmutableArray<ParameterSpecifier>.Empty,
                { Length: <= 0 } => ImmutableArray<ParameterSpecifier>.Empty,
                { }arr => ExtractValuesFrom(arr, nameof(parameters))
            };
            return new(owningTypeName, returnTypeSpecifier, inputParams, actualMethodName);
        }

        /// <inheritdoc />
        public override ImmutableArray<ParameterSpecifier> InputParameterList => _inputParameters;

        /// <inheritdoc />
        public override string OperationName => _methodName;

        /// <inheritdoc />
        public override bool IsMethod => true;

        private StaticMethodSpecifier(Type owningType, ParameterSpecifier returnParameter,
            ImmutableArray<ParameterSpecifier> inputParameters, string methodName) :
            base(owningType, returnParameter)
        {
            _inputParameters = inputParameters.ValueOrThrowIfDefault(nameof(inputParameters));
            _methodName = (methodName ?? throw new ArgumentNullException(nameof(methodName))).Trim();
            if (string.IsNullOrWhiteSpace(_methodName))
                throw new ArgumentException("A whitespace-only or empty string is not a permitted method name.");
            _stringRep = new LocklessLazyWriteOnce<string>(GetStringRep);
        }

        /// <inheritdoc />
        protected override bool IsImplEqualTo(StaticOperationSpecifier? other) =>
            TheMethodNameComparer.Equals(_methodName, (other as StaticMethodSpecifier)?._methodName);
        /// <inheritdoc />
        protected override int GetImplHashCode() => TheMethodNameComparer.GetHashCode(OperationName);
        /// <inheritdoc />
        protected override string GetImplStringRep() => _stringRep.Value;
        
        private string GetStringRep()
        {
            StringBuilder sb = new ("Input parameter list: \"");
            if (_inputParameters.IsDefaultOrEmpty)
            {
                sb.Append("EMPTY LIST");
            }
            else
            {
                sb.Append("{\"");
                sb.Append(_inputParameters.First().ToString());
                sb.Append("\"");
                if (_inputParameters.Length > 1)
                {
                    sb.Append(", ");
                }

                for (int i = 1; i < _inputParameters.Length; ++i)
                {
                    sb.Append("\"");
                    sb.Append(_inputParameters[i].ToString());
                    sb.Append("\"");
                    if (i < _inputParameters.Length - 1)
                        sb.Append(", ");
                }
                sb.AppendLine("}.");
            }
            return sb.ToString();
        }

      

        private readonly LocklessLazyWriteOnce<string> _stringRep;
        private readonly ImmutableArray<ParameterSpecifier> _inputParameters;
        private readonly string _methodName;
        private static readonly TrimmedStringComparer TheMethodNameComparer = TrimmedStringComparer.TrimmedOrdinal;
    }
}
