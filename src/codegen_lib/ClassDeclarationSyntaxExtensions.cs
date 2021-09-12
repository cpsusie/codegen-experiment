using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cjm.CodeGen
{
    internal static class ClassDeclarationSyntaxExtensions
    {
        public static int Compare(ClassDeclarationSyntax? l, ClassDeclarationSyntax? r) => TheComparer.Compare(l, r);

        public static ImmutableSortedDictionary<ClassDeclarationSyntax, TValue>.Builder CreateCdsSortedDicBldr<TValue>() => ImmutableSortedDictionary.CreateBuilder<ClassDeclarationSyntax, TValue>(TheComparer);

        public static ImmutableSortedDictionary<ClassDeclarationSyntax, ImmutableArray<TValue>.Builder>.Builder
            CreateCdsSortedDicArrayBldr<TValue>()  => ImmutableSortedDictionary
            .CreateBuilder<ClassDeclarationSyntax, ImmutableArray<TValue>.Builder>(TheComparer);

        private static readonly CdsComparer TheComparer = CdsComparer.GetComparer();
    }
}