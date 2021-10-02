using System;

namespace Cjm.Templates.Attributes
{
    [AttributeUsage(AttributeTargets.Interface)]
    public sealed class CjmTemplateInterfaceAttribute : Attribute
    {
        public const string ShortName = "CjmTemplateInterface";
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class CjmTemplateImplementationAttribute : Attribute
    {
        public const string ShortName = "CjmTemplateImplementation";
        public Type ImplementsInterfaceSpecifiedBy { get; }

        public CjmTemplateImplementationAttribute(Type t) =>
            ImplementsInterfaceSpecifiedBy = t ?? throw new ArgumentNullException(nameof(t));
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class CjmTemplateInstantiationAttribute : Attribute
    {
        public const string ShortName = "CjmTemplateInstantiation";
        public Type ImplementedTemplateInterface { get; }

        public CjmTemplateInstantiationAttribute(Type implements) => ImplementedTemplateInterface =
            implements ?? throw new ArgumentNullException(nameof(implements));

    }
}
