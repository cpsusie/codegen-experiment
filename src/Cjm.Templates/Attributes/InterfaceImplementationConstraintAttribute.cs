using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using HpTimeStamps;
using JetBrains.Annotations;

namespace Cjm.Templates.Attributes
{
    [AttributeUsage(AttributeTargets.Interface)]
    public abstract class InterfaceImplementationConstraintAttribute : ConstraintAttribute
    {

    }
}