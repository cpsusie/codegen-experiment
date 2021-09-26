using System;
using System.Collections.Generic;
using System.Text;

namespace Cjm.Templates.Attributes
{
    [AttributeUsage(AttributeTargets.Interface)]
    public sealed class CjmTemplateInterfaceAttribute : Attribute
    {
        
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class CjmTemplateImplementationAttribute : Attribute
    {
        public Type ImplementsInterfaceSpecifiedBy { get; }

        public CjmTemplateImplementationAttribute(Type t) =>
            ImplementsInterfaceSpecifiedBy = t ?? throw new ArgumentNullException(nameof(t));
    }
}
