using System;
using System.Collections.Generic;
using System.Text;

namespace Cjm.Templates.Exceptions
{
    public abstract class ArgumentOutOfRangeExceptionBase<T> : ArgumentOutOfRangeException where T : notnull
    {
        public sealed override object ActualValue { get; }
        public T Value { get; }

        protected ArgumentOutOfRangeExceptionBase(T actualValue, string message, string? paramName) : base(paramName,
            actualValue ?? throw new ArgumentNullException(nameof(actualValue)),
            message ?? throw new ArgumentNullException(nameof(message)))
        {
            Value = actualValue;
            ActualValue = actualValue;
        }
    }

    public sealed class ArgumentNegativeException<T> : ArgumentOutOfRangeExceptionBase<T> where T : notnull
    {
        public ArgumentNegativeException(T negativeValue, string paramName) : base(negativeValue,
            CreateMessage(negativeValue ?? throw new ArgumentNullException(nameof(negativeValue)),
                paramName ?? throw new ArgumentNullException(nameof(paramName))), paramName) {}

        private static string CreateMessage(T negativeValue, string paramName) 
            => $"The parameter named \"{paramName}\" may not be negative.  Actual value: {negativeValue}.";
    }

    public sealed class ArgumentNotPositiveException<T> : ArgumentOutOfRangeExceptionBase<T> where T : notnull
    {
        public ArgumentNotPositiveException(T nonPositiveValue, string paramName) : base(nonPositiveValue,
            CreateMessage(nonPositiveValue ?? throw new ArgumentNullException(nameof(nonPositiveValue)),
                paramName ?? throw new ArgumentNullException(nameof(paramName))), paramName) { }

        private static string CreateMessage(T negativeValue, string paramName)
            => $"The parameter named \"{paramName}\" must be positive.  Actual value: {negativeValue}.";
    }

    public sealed class ArgumentOutOfRangeException<T> : ArgumentOutOfRangeExceptionBase<T> where T : notnull
    {
        public T InclusiveMinimum { get; }
        public T InclusiveMaximum { get; }

        public ArgumentOutOfRangeException(T outOfRangeValue, string paramName, T minimum, T maximum) : base(
            outOfRangeValue,
            CreateMessage(outOfRangeValue ?? throw new ArgumentNullException(nameof(outOfRangeValue)),
                paramName ?? throw new ArgumentNullException(nameof(paramName)),
                minimum ?? throw new ArgumentNullException(nameof(minimum)),
                maximum ?? throw new ArgumentNullException(nameof(maximum))), paramName)
        {
            InclusiveMaximum = maximum;
            InclusiveMinimum = minimum;
        }

        private static string CreateMessage(T outOfRangeValue, string paramName, T minimum, T maximum) =>
            $"The parameter named \"{paramName}\" must be greater than or equal to [{minimum}] and less than " +
            $"or equal to [{maximum}].  Actual value: [{outOfRangeValue}].";
    }
}
