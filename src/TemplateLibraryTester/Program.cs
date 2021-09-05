using System;
using System.Diagnostics;
using HpTimeStamps;
using TemplateLibrary;

namespace TemplateLibraryTester
{
    using BlAttrib = BetterListExtenderNeedInAttrib<BetterList<PortableMonotonicStamp>>;
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Hello World!");
            ExamplePmsFast.Example();
            Type collectionType = BlAttrib.Collection;
            Type enumerator = BlAttrib.Enumerator;
            Type itemType = BlAttrib.Item;
            Console.WriteLine($"Collection type: {collectionType.FullName}");
            Console.WriteLine($"Enumerator type: {enumerator.FullName}");
            Console.WriteLine($"Item type: {itemType.FullName}");
        }
    }
}
