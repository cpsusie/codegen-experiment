using System;
using System.Collections.Generic;
using Cjm.CodeGen.Attributes;
using HpTimeStamps;

namespace Tricksie
{
    public static partial class AnotherProgram
    {
        const int Wrong = 3;
    }
}

namespace FromDoodle
{
    public static partial class AnotherProgram
    {
        const int Wrong = 2;
    }

    // ReSharper disable All
    namespace Cjm.Test
    {
        class Program
        {
            public static void Main()
            {
                Console.WriteLine();
            }
        }

        [EnableAugmentedEnumerationExtensions(typeof(List<PortableMonotonicStamp>))]
        public static partial class AnotherProgram
        {
            public const int Five = 5;
        }


        [EnableAugmentedEnumerationExtensions(typeof(List<PortableMonotonicStamp>))]
        public static class Impartial
        {
            public const int Six = 6;
        }

        [EnableAugmentedEnumerationExtensions(typeof(List<PortableMonotonicStamp>))]
        public partial class TooDynamic
        {
            public const int Seven = 7;
        }

        [EnableAugmentedEnumerationExtensions(typeof(List<PortableMonotonicStamp>))]
        internal partial class TooSecretive
        {
            public const int Eight = 8;
        }
    }
}