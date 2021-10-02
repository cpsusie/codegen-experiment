using System;

namespace Cjm.Templates.Example
{
    /// <summary>
    /// Exception throw to indicate that list's capacity cannot be further increased
    /// </summary>
    public sealed class ListFullException : InvalidOperationException
    {
        internal ListFullException(string s, OverflowException? e) : base(
            s ?? throw new ArgumentNullException(nameof(s)), e)
        {
        }
    }
}