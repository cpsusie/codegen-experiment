using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using HpTimeStamps;
using JetBrains.Annotations;

namespace Cjm.Templates.Attributes
{
    [AttributeUsage(AttributeTargets.Interface)]
    public abstract class InterfaceImplementationConstraintAttribute : ConstraintAttribute
    {

    }
}