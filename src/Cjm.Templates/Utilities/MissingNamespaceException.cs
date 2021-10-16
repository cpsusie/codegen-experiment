using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cjm.Templates.Utilities;

internal sealed class MissingNamespaceException : ApplicationException
{
    public MissingNamespaceException(TypeDeclarationSyntax typeToSearchFor,
        string name) : base(CreateMessage(
        typeToSearchFor ?? throw new ArgumentNullException(nameof(typeToSearchFor)),
        name ?? throw new ArgumentNullException(nameof(name)))) {}

    private static string CreateMessage(TypeDeclarationSyntax typeToSearchFor, string name) =>
        $"Cannot find namespace for type {typeToSearchFor.Identifier.Text} in template implementation {name}.";
}