using System;
using Cjm.CodeGen.Attributes;

namespace TestApp
{
    [FastLinqExtensions]
    public static class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }

    public static partial class ListOfPortableDurationExtender<List<PortableDuration>>
    {

    }
}
