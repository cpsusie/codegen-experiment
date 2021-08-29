using System;
using System.Threading;
using Cjm.CodeGen.Attributes;
using LoggerLibrary;
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
            using var eel = LoggerSource.Logger.CreateEel(nameof(TransformEnumeratorGenerator), nameof(Execute), context.ToString() ?? "NONE");
            try
            {
                CancellationToken token = context.CancellationToken;
                if (context.SyntaxReceiver is EnableFastLinqClassDeclSyntaxReceiver
                {
                    ClassToAugment: { } augmentSyntax
                })
                {
                    token.ThrowIfCancellationRequested();
                    LoggerSource.Logger.LogMessage($"Examining syntax: [{augmentSyntax.ToString()}]" );
                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                LoggerSource.Logger.LogException(ex);
                throw;
            }
        }
    } 

    public sealed class EnableFastLinqClassDeclSyntaxReceiver : ISyntaxReceiver
    {
        public ClassDeclarationSyntax? ClassToAugment { get; private set; }

        public void OnVisitSyntaxNode(SyntaxNode syntax)
        {
            using var eel = LoggerSource.Logger.CreateEel(nameof(EnableFastLinqClassDeclSyntaxReceiver),
                nameof(OnVisitSyntaxNode), syntax.ToString());
            if (syntax is ClassDeclarationSyntax cds)
            {
                //LoggerSource.Logger.LogMessage($"Examining class declaration syntax: [{cds.ToString()}].");
                bool isPublicStatic = IsPublicStaticClassDeclaration(cds);
                bool hasFastLinkExtensionAttribute = HasFastLinkExtensionsAttribute(cds);
                /*LoggerSource.Logger.LogMessage("\tThe class " + ((isPublicStatic, hasFastLinkExtensionAttribute) switch
                {
                    (false, false) => "is not public static and does not have the fast link extension attribute.",
                    (false, true) => "is not public static but does have the fast link extension attribute.",
                    (true, false) => "is public static but does not have the fast link extension attribute.",
                    (true, true) => "is public static and does have the fast link extension attribute.",
                    
                }));*/
//#if DEBUG
//                if (isPublicStatic)
//                    Debugger.Launch();
//#endif
                if (isPublicStatic && hasFastLinkExtensionAttribute)
                {
                    ClassToAugment = cds;
                    LoggerSource.Logger.LogMessage(
                        $"\t Class declaration syntax {cds} is selected for semantic analysis.");
                }
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
                    
                    if (attrib.Name.ToString() == FastLinqExtensionsAttribute.ShortName)
                        return true;
                }
            }
            return false;
        }
    }

    internal static class LoggerSource
    {
        public static readonly ICodeGenLogger Logger = CodeGenLogger.Logger;
    }
}
