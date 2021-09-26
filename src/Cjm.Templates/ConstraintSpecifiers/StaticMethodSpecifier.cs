using System;
using Cjm.Templates.Utilities.SetOnce;
using LoggerLibrary;

namespace Cjm.Templates.ConstraintSpecifiers
{
    public sealed class StaticMethodSpecifier : StaticOperationSpecifier
    {
        public static StaticMethodSpecifier CreateStaticMethodSpecifier(string methodName,
            Type formDelegate)
        {
            string actualMethodName = (methodName ?? throw new ArgumentNullException(nameof(methodName))).Trim() switch
            {
                { } txt when string.IsNullOrWhiteSpace(txt) => throw new ArgumentException(
                    "Parameter may not be empty or whitespace-only.", nameof(methodName)),
                { } mn => mn
            };
            return new(formDelegate ?? throw new ArgumentNullException(nameof(formDelegate)), actualMethodName);
        }

        public sealed override Type OperationFormDelegate => _delegateFormType;

        /// <inheritdoc />
        public override string OperationName => _methodName;

        /// <inheritdoc />
        public override bool IsMethod => true;

        private StaticMethodSpecifier(Type delegateType, string methodName)
        {
            _delegateFormType = delegateType ?? throw new ArgumentNullException(nameof(delegateType));
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

        private string GetStringRep() => $"Operation Form Delegate: [{_delegateFormType.Name}]";
        


        private readonly Type _delegateFormType;
        private readonly LocklessLazyWriteOnce<string> _stringRep;
        private readonly string _methodName;
        private static readonly TrimmedStringComparer TheMethodNameComparer = TrimmedStringComparer.TrimmedOrdinal;
    }
}
