using System;
using System.Collections.Immutable;
using Cjm.Templates.Utilities.SetOnce;

namespace Cjm.Templates.Utilities
{
    internal static class ImmutableArrayExtensions
    {
        public static ImmutableArray<T> ValueOrThrowIfDefault<T>(this ImmutableArray<T> checkMe, string? paramName) =>
            checkMe.IsDefault
                ? throw new UninitializedStructArgumentException<ImmutableArray<T>>(paramName ?? nameof(checkMe))
                : checkMe;
        public static ImmutableArray<T> ValueOrEmptyIfDefault<T>(this ImmutableArray<T> checkMe) =>
            checkMe.IsDefault ? ImmutableArray<T>.Empty : checkMe;

    }

    public abstract class UninitializedStructArgumentException : ArgumentException
    {
        public abstract Type OffendingStructType { get; }
        protected Type ConcreteType => _concreteType.ConcreteType;

        protected UninitializedStructArgumentException(string message, string? parameterName, Exception? inner) : base(
            message ?? throw new ArgumentNullException(nameof(message)),
            parameterName, inner) => _concreteType = new(this);


        private readonly LocklessConcreteType _concreteType;
    }

    public sealed class UninitializedStructArgumentException<T> : UninitializedStructArgumentException where T : struct
    {
        /// <inheritdoc />
        public override Type OffendingStructType => typeof(T);

        public UninitializedStructArgumentException(string? parameterName, string? extraInfo, Exception? inner) : base(
            CreateMessage(parameterName, extraInfo, inner), parameterName, inner) { }

        public UninitializedStructArgumentException(string? parameterName) 
            : this(parameterName, null, null) {}
        public UninitializedStructArgumentException(string? parameterName, Exception? inner) 
            : this(parameterName, null, inner){}
        public UninitializedStructArgumentException(string? parameterName, string? extraInfo) 
            : this(parameterName, extraInfo, null){}
        public UninitializedStructArgumentException() 
            : this(null,null,null){}

        private static string CreateMessage(string? paramName, string? extraInfo, Exception? inner)
        {
            const string msgFormat = "The supplied parameter{0}of type {1} was not properly initialized.{2}{3}";
            string paramNameMsg = !string.IsNullOrWhiteSpace(paramName) ? $" (name: {paramName}) " : " ";
            string extraInfoMsg = !string.IsNullOrWhiteSpace(extraInfo)
                ? $" Additional information: \"{extraInfo}\"."
                : string.Empty;
            string consultInnerMessage = inner == null ? string.Empty : " Consult inner exception for details.";
            return string.Format(msgFormat, paramNameMsg, typeof(T).Name, extraInfoMsg, consultInnerMessage);
        }
    }
}
