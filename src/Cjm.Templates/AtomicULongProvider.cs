using System.Diagnostics;
using System.Threading;

namespace Cjm.Templates
{
    internal sealed class AtomicULongProvider
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public ulong NextValue
        {
            get
            {
                long l = Interlocked.Increment(ref _value);
                return unchecked((ulong)l);
            }
        }

        public ulong CurrentValue
        {
            get
            {
                long l = Interlocked.Read(ref _value);
                return unchecked((ulong) l);
            }
        }

        public sealed override string ToString() =>
            $"[{nameof(AtomicULongProvider)}] -- Current value: [{CurrentValue:N}].";
        
        private long _value = 0;
    }
}