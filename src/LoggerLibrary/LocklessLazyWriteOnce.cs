using System;
using System.Diagnostics;
using System.Threading;

namespace LoggerLibrary
{
    public struct LocklessSetOnlyFlag
    {
        public readonly bool IsSet
        {
            get
            {
                int val = _value;
                return val == Set;
            }
        }

        public bool TrySet()
        {
            return Interlocked.CompareExchange(ref _value, Set, NotSet) == NotSet;
        }

        public void SetOrThrow()
        {
            bool set = TrySet();
            if (!set)
            {
                throw new InvalidOperationException("The flag has already been set.");
            }
        }

        private volatile int _value;
        private const int NotSet = 0;
        private const int Set = 1;
    }

    internal sealed class LocklessLazyWriteOnce<T> where T : class
    {
        public T Value
        {
            get
            {
                T? ret = _value;
                
                if (ret == null)
                {
                    bool iSwappedIt = false;
                    bool ok;
                    T? newVal;
                    do
                    {
                        (ok, newVal) = Generate();
                        ret = _value;
                    } while (ret == null && !ok);

                    if (ret == null)
                    {
                        iSwappedIt = Interlocked.CompareExchange(ref _value, newVal, null) == null;
                        ret = _value;
                    }

                    if (iSwappedIt)
                    {
                        Interlocked.Exchange(ref _init, null);
                    }
                }
                Debug.Assert(ret != null);
                return ret!;
            }
        }

        public bool TrySetAlternateValue(T alternateValue)
        {
            if (alternateValue == null) throw new ArgumentNullException(nameof(alternateValue));
            bool swappedIt = Interlocked.CompareExchange(ref _value, alternateValue, null) == null;
            Debug.Assert(_value != null);
            if (swappedIt)
            {
                Interlocked.Exchange(ref _init, null);
            }
            return swappedIt;
        }

        public bool IsSet
        {
            get
            {
                T? ret = _value;
                return ret != null;
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            bool isSet = IsSet;
            return isSet ? $"[{nameof(LocklessLazyWriteOnce<T>)}]-- Value: {Value}" : $"[{nameof(LocklessLazyWriteOnce<T>)}]-- NOT SET";
        }

        internal LocklessLazyWriteOnce(Func<T> initializer)
            => _init = initializer ?? throw new ArgumentNullException(nameof(initializer));
        private (bool Ok,  T? Value) Generate()
        {
            try
            {
                Func<T>? init = _init;
                T? ret = init?.Invoke();
                return (ret != null, ret);
            }
            catch (Exception)
            {
                return (false, null);
            }
        }

        private volatile T? _value;
        private volatile Func<T>? _init;
    }
}