using System.Threading;

namespace LoggerLibrary
{
    public struct LocklessSetOnlyRefStr
    {
        public bool IsSet
        {
            get
            {
                int val = _value;
                return val == Set;
            }
        }

        public bool TrySet()
        {
            const int wantToBe = Set;
            const int needToBeNow = Clear;
            return Interlocked.CompareExchange(ref _value, wantToBe, needToBeNow) == needToBeNow;
        }

        private volatile int _value;
        private const int Clear = 0;
        private const int Set = 1;
    }
}