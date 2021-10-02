using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cjm.Templates.Utilities.SetOnce;

namespace Cjm.Templates.Utilities
{
    internal sealed class LocklessFreezeFlag
    {
        public bool IsFrozen => Code == FreezeFlagCode.Frozen;
        public bool IsBeingFrozen => Code == FreezeFlagCode.Freezing;
        public bool NotFrozen => Code == FreezeFlagCode.Clear;

        public FreezeFlagCode Code
        {
            get
            {
                int val = Volatile.Read(ref _code);
                return (FreezeFlagCode)val;
            }
        }

        public bool TryBeginFreeze() => ControlledSetFlag(FreezeFlagCode.Freezing, FreezeFlagCode.Clear);

        public void FinishFreezeOrThrow()
        {
            if (!ControlledSetFlag(FreezeFlagCode.Frozen, FreezeFlagCode.Freezing))
            {
                throw new LocklessMultiStepException(
                    $"Flag was not in the {nameof(FreezeFlagCode.Freezing)} state at moment of call to {nameof(FinishFreezeOrThrow)}.");
            }
        }

        public void CancelFreezeOrThrow()
        {
            if (!ControlledSetFlag(FreezeFlagCode.Clear, FreezeFlagCode.Freezing))
            {
                throw new LocklessMultiStepException(
                    $"Flag was not in the {nameof(FreezeFlagCode.Freezing)} state at moment of call to {nameof(CancelFreezeOrThrow)}.");
            }
        }

        private bool ControlledSetFlag(FreezeFlagCode wantToBe, FreezeFlagCode needToBeNow)
        {
            int desired = (int)wantToBe;
            int mustBe = (int)needToBeNow;
            return Interlocked.CompareExchange(ref _code, desired, mustBe) == mustBe;
        }

        /// <inheritdoc />
        public override string ToString() => $"[{nameof(LocklessFreezeFlag)}] -- State: [" + Code + "]."; 
        

        private int _code = (int)FreezeFlagCode.Clear;
    }

    public enum FreezeFlagCode
    {
        Clear = 0,
        Freezing,
        Frozen
    }
}
