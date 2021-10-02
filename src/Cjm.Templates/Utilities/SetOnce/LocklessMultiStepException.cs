using System;

namespace Cjm.Templates.Utilities.SetOnce
{
    /// <summary>
    /// Class signals a serious logic problem in a multistep lockless flag operation sequence
    /// </summary>
    public sealed class LocklessMultiStepException : InvalidOperationException
    {
        internal LocklessMultiStepException(string message) : this(message, null){}
        internal LocklessMultiStepException(string message, Exception? inner) 
            : base(message ?? throw new ArgumentNullException(nameof(message)), inner) {}
    }
}