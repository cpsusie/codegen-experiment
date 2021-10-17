using System;
using System.Diagnostics;
using System.Threading;
using Cjm.Templates.Utilities.SetOnce;

namespace Cjm.Templates;

internal struct LocklessNonZeroInteger
{
    public readonly bool IsSet
    {
        get
        {
            int val = _value;
            return val != 0;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public readonly int Value => IsSet ? _value : throw new InvalidOperationException("The value has not been set yet.");

    public bool TrySet(int wantToBe)
    {
        if (wantToBe == 0) throw new ArgumentException(@"Value cannot be zero.", nameof(wantToBe));
        const int needToBeNow = 0;
        return Interlocked.CompareExchange(ref _value, wantToBe, needToBeNow) == needToBeNow;
    }

    public void SetOrThrow(int wantToBe)
    {
        if (wantToBe == 0) throw new ArgumentException(@"Value cannot be zero.", nameof(wantToBe));
        if (!TrySet(wantToBe))
        {
            throw new LocklessFlagAlreadySetException<int>(wantToBe, _value);
        }
    }

    public override readonly string ToString() => $"[{nameof(LocklessNonZeroInteger)}] -- " +
                                                  (IsSet ? $"\tValue: \t[{_value}]." : "[NOT SET].");
    private volatile int _value;
}