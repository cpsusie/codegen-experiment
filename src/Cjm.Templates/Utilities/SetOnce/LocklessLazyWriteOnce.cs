using System;
using System.Diagnostics;
using System.Threading;
using HpTimeStamps;
using JetBrains.Annotations;

namespace Cjm.Templates.Utilities.SetOnce
{
    public sealed class LocklessLazyWriteOnceValueType<T> where T : struct
    {
        public bool IsSet
        {
            get
            {
                SettingCode value = (SettingCode) Volatile.Read(ref _state);
                return value == SettingCode.Set;
            }
        }

        private SettingCode State
        {
            get
            {
                int value = Volatile.Read(ref _state);
                return (SettingCode)value;
            }
        }

        public T Value
        {
            get
            {
                var state = (SettingCode)_state;
                if (state == SettingCode.Set)
                {
                    return _value;
                }

                state = State;
                while (state != SettingCode.Set)
                {
                    if (TryBegin())
                    {

                        try
                        {
                            _value = _factory();
                            FinishOrThrow();
                        }
                        catch (LocklessMultiStepException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            CancelOrThrow();
                            throw new DelegateThrewException(nameof(_factory), _factory, ex);
                        }
                    }
                    state = State;
                }
                Debug.Assert(state == SettingCode.Set);
                return _value;
            }
        }

        public LocklessLazyWriteOnceValueType(Func<T> lazyInitFactory) =>
            _factory = lazyInitFactory ?? throw new ArgumentNullException(nameof(lazyInitFactory));

        public bool TrySet(T setMe)
        {
            if (TryBegin())
            {
                _value = setMe;
                FinishOrThrow();
                return true;
            }
            return false;
        }

        /// <inheritdoc />
        public override string ToString() => $"[{nameof(LocklessLazyWriteOnceValueType<T>)}] -- " + State switch
        {
            SettingCode.Setting => "[SET IN PROGRESS].",
            SettingCode.Set => $"Value: [{_value.ToString()}].",
            _ => "[NOT SET]."
        };

        private bool TryBegin()
        {
            const int wantToBe = (int) SettingCode.Setting;
            const int needToBeNow = (int) SettingCode.Clear;
            return Interlocked.CompareExchange(ref _state, wantToBe, needToBeNow) == needToBeNow;
        }

        private bool TryFinish()
        {
            const int wantToBe = (int) SettingCode.Set;
            const int needToBeNow = (int) SettingCode.Setting;
            return Interlocked.CompareExchange(ref _state, wantToBe, needToBeNow) == needToBeNow;
        }

        private bool TryCancel()
        {
            const int wantToBe = (int) SettingCode.Clear;
            const int needToBeNow = (int) SettingCode.Setting;
            return Interlocked.CompareExchange(ref _state, wantToBe, needToBeNow) == needToBeNow;
        }

        private void FinishOrThrow()
        {
            if (!TryFinish())
                throw new LocklessMultiStepException($"The state was not {nameof(SettingCode.Setting)} at moment of call.");
        }

        private void CancelOrThrow()
        {
            if (!TryCancel())
                throw new InvalidOperationException($"The state was not {nameof(SettingCode.Setting)} at moment of call.");
        }
        
        private readonly Func<T> _factory;
        private int _state;
        private T _value;

        private enum SettingCode 
        {
            Clear = 0,
            Setting = 1,
            Set = 2
        }
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

        public bool TrySetNonDefault(T value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return Interlocked.CompareExchange(ref _value, value, null) == null;
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

        

        public void SetNonDefaultValueOrThrow(T nonDefault, bool forgiveIfAlready)
        {
            if (nonDefault == null) throw new ArgumentNullException(nameof(nonDefault));
            if (!TrySetNonDefault(nonDefault))
            {
                T? value = Volatile.Read(ref _value);
                Debug.Assert(value != null);
                if (!forgiveIfAlready || !ReferenceEquals(value, nonDefault))
                    throw new LocklessFlagAlreadySetException<T>(nonDefault, value!);
            }
        }

        /// <inheritdoc />
        public override string ToString() =>
            $"{nameof(LocklessLazyWriteOnce<T>)} -- value: [" + Value + "].";

        internal string DebuggerValue => $"{nameof(LocklessLazyWriteOnce<T>)} -- value: [" + (IsSet ? Value.ToString() : "NOT SET") + "].";

        private readonly Func<T> _generator;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private T? _value;
    }

    public class LocklessFlagAlreadySetException<[NotNull] T> : LocklessFlagAlreadySetException
    {
        public T DesiredValue { get; }

        public T AlreadySetValue { get; }

        /// <inheritdoc />
        protected sealed override Type LocklessFlagItemType => typeof(T);

        public LocklessFlagAlreadySetException(T desiredNonDefaultValue, T alreadySetValue) 
            : this(desiredNonDefaultValue, alreadySetValue, null) {}

        public LocklessFlagAlreadySetException(T desiredNonDefaultValue, T alreadySetValue, Exception? inner) : base(
            CreateMessage(desiredNonDefaultValue ?? throw new ArgumentNullException(nameof(desiredNonDefaultValue)),
                alreadySetValue ?? throw new ArgumentNullException(nameof(alreadySetValue)), inner), inner)
        {
            DesiredValue = desiredNonDefaultValue;
            AlreadySetValue = alreadySetValue;
        }

        private static string CreateMessage(T desiredNonDefaultValue, T alreadySetValue, Exception? inner)
        {
            return
                $"Unable to set lockless flag's value to non default ({desiredNonDefaultValue}) -- value is already set to be ({alreadySetValue})." +
                (inner != null ? " Consult inner exception for details." : string.Empty);
        }


    }

    public abstract class LocklessFlagAlreadySetException : InvalidOperationException
    {
        protected abstract Type LocklessFlagItemType { get; }
        
        protected LocklessFlagAlreadySetException(string message, Exception? inner) 
            : base(message ?? throw new ArgumentNullException(nameof(message)), inner) {}
    }
}