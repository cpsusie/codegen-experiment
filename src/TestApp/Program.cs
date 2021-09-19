using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cjm.CodeGen.Attributes;
using HpTimeStamps;
using MonotonicContext = HpTimeStamps.MonotonicStampContext;
namespace TestApp
{
    using MonotonicStamp = HpTimeStamps.MonotonicTimeStamp<MonotonicContext>;
    using StampSource = HpTimeStamps.MonotonicTimeStampUtil<MonotonicContext>;
    public static class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            ListOfPortableDurationExtender.WrappedListOfPortableMonotonicStampByVal wrapper;
            int total;
            {
                MonotonicStamp baseline = StampSource.StampNow;
                var list =
                    new List<PortableMonotonicStamp>();
                list.Add((PortableMonotonicStamp)baseline);
                list.Add((PortableMonotonicStamp) (baseline + Duration.FromDays(10_941.32)));
                PortableMonotonicStamp final = (PortableMonotonicStamp) (baseline - Duration.FromDays(142_365.12345));
                //PortableMonotonicStamp final = list[0] - PortableDuration.FromDays(142_365.12345);
                Debug.Assert(final < list[0]);
                list.Add(final);
                wrapper = list;
                total = list.Count;
            }
            int itmCount = 0;
            int wrapperCount = total;

            foreach (var item in wrapper)
            {
                Console.WriteLine("Item # {0} of {1}: \t[{2}].", ++itmCount, wrapperCount, item.ToString());
            }
        }

    }
    
    [EnableAugmentedEnumerationExtensions(typeof(List<PortableMonotonicStamp>))]
    public static partial class ListOfPortableDurationExtender
    {

    }
}
