using System;
using System.Collections.Generic;
using System.Text;

namespace Cjm.CodeGen.Attributes
{
    [AttributeUsage(AttributeTargets.GenericParameter)]
    public sealed class RoRefSuitableVtAttribute : Attribute
    {
        public const string ShortName = "RoRefSuitableVt";
    }

    [AttributeUsage(AttributeTargets.GenericParameter)]
    public sealed class ClassNotInterfaceAttribute : Attribute
    {
        public const string ShortName = "NotInterface";
    }

    [AttributeUsage(AttributeTargets.GenericParameter)]
    public sealed class RefTypeOrSmallValueTypeAttribute : Attribute
    {
        public const string ShortName = "ClassOrSmallStruct";
    }

}
