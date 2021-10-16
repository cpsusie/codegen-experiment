using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cjm.Templates.Utilities;

internal readonly record struct SubPair(TypeParameterSyntax ToBeReplaced, ITypeSymbol ReplaceWithMe)
{
    public bool Equals(SubPair other) => ToBeReplaced.Equals(other.ToBeReplaced) &&
                                         SymbolEqualityComparer.Default.Equals(ReplaceWithMe,
                                             other.ReplaceWithMe);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        int hash = ToBeReplaced.GetHashCode();
        unchecked
        {
            hash = (hash * 397) ^ SymbolEqualityComparer.Default.GetHashCode(ReplaceWithMe);
        }
        return hash;
    }
        
}