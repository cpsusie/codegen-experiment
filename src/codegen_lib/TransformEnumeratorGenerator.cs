using System;
using System.Data;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cjm.CodeGen
{
    [Generator]
    public sealed class TransformEnumeratorGenerator : ISourceGenerator
    {
        /// <inheritdoc />
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new EnableFastLinqClassDeclSyntaxReceiver());
        }

        /// <inheritdoc />
        public void Execute(GeneratorExecutionContext context)
        {
            using var eel = Logger.CreateEel(nameof(TransformEnumeratorGenerator), nameof(Execute), context.ToString() ?? "NONE");
            try
            {
                CancellationToken token = context.CancellationToken;
                if (context.SyntaxReceiver is EnableFastLinqClassDeclSyntaxReceiver
                {
                    ClassToAugment: { } augmentSyntax
                })
                {
                    Logger.LogMessage($"Examining syntax: [{augmentSyntax.ToString()}]" );
                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception)
            {
                throw;
            }
        }

        private static readonly ICodeGenLogger Logger = CodeGenLogger.Logger;
    } 

    public sealed class EnableFastLinqClassDeclSyntaxReceiver : ISyntaxReceiver
    {
        public ClassDeclarationSyntax? ClassToAugment { get; private set; }

        public void OnVisitSyntaxNode(SyntaxNode syntax)
        {
            if (syntax is ClassDeclarationSyntax cds && IsPublicStaticClassDeclaration(cds) && HasFastLinkExtensionsAttribute(cds))
            {
                ClassToAugment = cds;
            }
        }

        static bool IsPublicStaticClassDeclaration(ClassDeclarationSyntax cds)
        {
            bool foundPublic = false;
            bool foundStatic = false;
            foreach (var modifier in cds.Modifiers)
            {
                if (modifier.IsKind(SyntaxKind.PublicKeyword))
                {
                    foundPublic = true;
                }

                if (modifier.IsKind(SyntaxKind.StaticKeyword))
                {
                    foundStatic = true;
                }

                if (foundStatic && foundPublic)
                {
                    return true;
                }
            }

            return foundStatic && foundPublic;
        }

        static bool HasFastLinkExtensionsAttribute(ClassDeclarationSyntax cds)
        {
            foreach (var attribList in cds.AttributeLists)
            {
                foreach (var attrib in attribList.Attributes)
                {
                    if (attrib.Name.Span.ToString() == FastLinqExtensionsAttribute.ShortName)
                        return true;
                }
            }
            return false;
        }
    }
}
