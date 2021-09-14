using System;

namespace Cjm.CodeGen
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class FoobarAttribute : Attribute
    {
        public Type TheType { get; }
        public FoobarAttribute(Type myType) => TheType = myType ?? throw new ArgumentNullException(nameof(myType));

    }
}