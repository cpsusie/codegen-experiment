using System;

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

        public Type TargetType { get; }

        public EnableAugmentedEnumerationExtensionsAttribute(Type affectedType) =>
            TargetType = affectedType ?? throw new ArgumentNullException(nameof(affectedType));
    }
}
