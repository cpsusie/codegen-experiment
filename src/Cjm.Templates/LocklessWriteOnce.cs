using System;
using System.Diagnostics;
using System.Threading;
using Cjm.Templates.Utilities.SetOnce;

namespace Cjm.Templates;

sealed class LocklessWriteOnce<T> where T : class
{
    public bool IsSet
    {
        get
        {
            T? test = Volatile.Read(ref _value);
            return test != null;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public T Value
    {
        get
        {
            T? test = _value;
            return test ?? throw new InvalidOperationException("The value has not yet been set.");
        }
    }

    public bool TrySet(T val)
    {
        if (val == null) throw new ArgumentNullException(nameof(val));
        return Interlocked.CompareExchange(ref _value, val, null) == null;
    }

    public void SetOrThrow(T val)
    {
        if (val == null) throw new ArgumentNullException(nameof(val));
        if (!TrySet(val))
        {
            Debug.Assert(_value != null);
            throw new LocklessFlagAlreadySetException<T>(val, _value!);
        }
    }

    /// <inheritdoc />
    public override string ToString() => $"[{nameof(LocklessWriteOnce<T>)}] -- " +
                                         (IsSet ? $"\tValue: \t[{Value}]." : "[NOT SET].");
        

    private T? _value;
}