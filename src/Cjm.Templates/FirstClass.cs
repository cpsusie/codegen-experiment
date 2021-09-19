using System;
using MonotonicContext = HpTimeStamps.MonotonicStampContext;

namespace Cjm.Templates
{
    using MonotonicStamp = HpTimeStamps.MonotonicTimeStamp<MonotonicContext>;
    using StampSource = HpTimeStamps.MonotonicTimeStampUtil<MonotonicContext>;
    using Duration = HpTimeStamps.Duration;
    using PortableDuration = HpTimeStamps.PortableDuration;
    using PortableStamp = HpTimeStamps.PortableMonotonicStamp;

    public class FirstClass
    {
        public string Foobar { get; }

        public FirstClass(string foobar)
        {
            Foobar = foobar ?? throw new ArgumentNullException(nameof(foobar));
        }
    }
}
