using System;
using System.Diagnostics;
using System.Threading;
using Cjm.CodeGen.Exceptions;
using HpTimeStamps;

namespace Cjm.CodeGen
{
    public sealed class LocklessWriteOnce<T> where T : class
    {
        public bool IsSet
        {
            get
            {
                T? val = Volatile.Read(ref _value);
                return val != null;
            }
        }

        public T Value
        {
            get
            {
                T? val = _value;
                if (val == null)
                {
                    val = Volatile.Read(ref _value);
                    if (val == null)
                    {
                        throw new SetOnceValNotSetException<T>(typeof(LocklessWriteOnce<T>));
                    }
                }
                return val;
            }
        }

        internal LocklessWriteOnce(){}

        public bool TrySet(T value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return Interlocked.CompareExchange(ref _value, value, null) == null;
        }

        public void SetOrThrow(T value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (!TrySet(value))
            {
                throw new SetOnceValAlreadySetException<T>(value, Value);
            }
        }

        /// <inheritdoc />
        public override string ToString() =>
            nameof(LocklessWriteOnce<T>) + " -- [" + (IsSet ? Value.ToString() : NotSet) + "].";

        private T? _value;
        private const string NotSet = "NOT SET";
    }

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
                if (value == null)
                {
                    value = Volatile.Read(ref _value);
                    if (value == null)
                    {
                        T newVal = GetValue();
                        Interlocked.CompareExchange(ref _value, newVal, null);
                        value = Volatile.Read(ref _value);
                    }
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