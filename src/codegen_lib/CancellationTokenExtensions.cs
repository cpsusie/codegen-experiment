using System.Threading;

namespace Cjm.CodeGen
{
    internal static class CancellationTokenExtensions
    {
        internal static bool TrueOrThrowIfCancellationRequested(this CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return true;
        }
    }
}