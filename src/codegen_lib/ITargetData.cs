using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cjm.CodeGen
{
    public interface ITargetData
    {
        ClassDeclarationSyntax ClassToAugment { get; }
        AttributeSyntax AttributeSyntax { get; }
    }
}