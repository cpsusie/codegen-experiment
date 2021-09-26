using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Cjm.Templates.Attributes;

namespace Cjm.Templates.ConstraintSpecifiers
{
    public abstract class BinaryOperatorSpecifier : StaticOperatorSpecifierBase
    {
        protected BinaryOperatorSpecifier(Type delegateForm, OperatorSpecifier specifier) : base(
            delegateForm, specifier) { }
    }

    
}