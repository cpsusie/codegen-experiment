using System;
using System.Diagnostics;
using System.Threading;
using HpTimeStamps;

namespace Cjm.Templates.Utilities.SetOnce
{
    [DebuggerDisplay("{DebuggerValue}")]
    public sealed class LocklessLazyWriteOnce<T> where T : class
    {
        public bool IsSet
        {
            get
            {
                T? value = Volatile.Read(ref _value);
                return value != null;
            }
        }

        public T Value
        {
            get
            {
                T? value = _value;
                if (value != null)
                {
                    return value;
                }

                value = Volatile.Read(ref _value);
                if (value == null)
                {
                    T newValue = GetValue();
                    Debug.Assert(newValue != null);
                    Interlocked.CompareExchange(ref _value, newValue, null);
                    value = Volatile.Read(ref _value);
                }
                Debug.Assert(value != null);
                return value!;
            }
        }

        private T GetValue()
        {
            try
            {
                T? ret = _generator();
                if (ret == null)
                {
                    throw new DelegateReturnedNullException(nameof(_generator), _generator);
                }

                return ret;
            }
            catch (DelegateReturnedNullException)
            {
                throw;
            }
            catch (Exception inner)
            {
                throw new DelegateThrewException(nameof(_generator), _generator, inner);
            }
        }

        internal LocklessLazyWriteOnce(Func<T> generator) =>
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));


        /// <inheritdoc />
        public override string ToString() =>
            $"{nameof(LocklessLazyWriteOnce<T>)} -- value: [" + Value + "].";

        internal string DebuggerValue => $"{nameof(LocklessLazyWriteOnce<T>)} -- value: [" + (IsSet ? Value.ToString() : "NOT SET") + "].";

        private readonly Func<T> _generator;
        private T? _value;
    }
}