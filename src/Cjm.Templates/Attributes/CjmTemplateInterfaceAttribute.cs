using System;
using System.Diagnostics;


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
        // ReSharper disable once MemberInitializerValueIgnored
        private readonly Type _instantiatedTemplate = default!;
        public const string ShortName = "CjmTemplateInstantiation";

        public Type InstantiatedTemplate => _instantiatedTemplate;

        public bool ReferencesTemplateInterface { get; }
        public bool ReferencesSpecificTemplateImplementation => !ReferencesTemplateInterface;

        public CjmTemplateInstantiationAttribute(Type instantiationTarget, bool targetIsTemplateInterface)
        {
            _instantiatedTemplate = instantiationTarget ?? throw new ArgumentNullException(nameof(instantiationTarget));
            ReferencesTemplateInterface = targetIsTemplateInterface;
            Debug.Assert(InstantiatedTemplate != null);
            Debug.Assert(ReferencesTemplateInterface == (_instantiatedTemplate != null));
            Debug.Assert(ReferencesSpecificTemplateImplementation == (_instantiatedTemplate != null));
        }

    }
}
