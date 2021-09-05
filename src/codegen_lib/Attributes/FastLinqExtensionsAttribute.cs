﻿using System;

namespace Cjm.CodeGen.Attributes
{
    [AttributeUsage(AttributeTargets.Class)] 
    public sealed class FastLinqExtensionsAttribute : Attribute
    {
        public const string ShortName = "FastLinqExtensions";
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class EnableAugmentedEnumerationExtensionsAttribute : Attribute
    {
        public const string ShortName = "EnableAugmentedEnumerationExtensions";
    }
}
