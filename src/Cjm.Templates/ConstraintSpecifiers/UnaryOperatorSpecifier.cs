using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Cjm.Templates.Attributes;

namespace Cjm.Templates.ConstraintSpecifiers
{
    public abstract class UnaryOperatorSpecifier : StaticOperatorSpecifierBase
    {
        protected UnaryOperatorSpecifier(Type delegateForm, OperatorSpecifier specifier) : base(delegateForm, specifier)
        {
            if (specifier.Form != OperatorForm.Unary)
                throw new ArgumentException(
                    $"Specifier must be of form {nameof(OperatorForm.Unary)} but it's actual value is {specifier.Form}.",
                    nameof(specifier));
        }
    }
}