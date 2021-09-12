using Cjm.CodeGen.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cjm.CodeGen
{
    public sealed class EnableAugmentedEnumerationExtensionSyntaxReceiver : CjmAttributeOnClassSyntaxReceiver<
        EnableAugmentedEnumerationExtensionTargetData>
    {
        /// <inheritdoc /> 
        protected override EnableAugmentedEnumerationExtensionTargetData? ExtractTargetDataFromNodeOrNot(SyntaxNode syntax)
        {
            EnableAugmentedEnumerationExtensionTargetData? ret = null;
            //using var eel = LoggerSource.Logger.CreateEel(nameof(EnableFastLinqClassDeclSyntaxReceiver),
            //    nameof(OnVisitSyntaxNode), syntax.ToString());
            if (syntax is ClassDeclarationSyntax cds)
            {
                //LoggerSource.Logger.LogMessage($"Examining class declaration syntax: [{cds.ToString()}].");
                bool isPublicStatic = IsPublicStaticPartialClassDeclaration(cds);
                var extensionAttribSyntax = FindExtensionsAttribute(cds, EnableAugmentedEnumerationExtensionsAttribute.ShortName);
                bool hasExtensionAttribSyntax = extensionAttribSyntax != null;
                TypeOfExpressionSyntax? tps = hasExtensionAttribSyntax ? FindFirstAttribArgIfTypeOf(extensionAttribSyntax!) : null;
                bool hasTps = tps?.Type != null;
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
                if (isPublicStatic && hasExtensionAttribSyntax && hasTps)
                {
                    ret = EnableAugmentedEnumerationExtensionTargetData.CreateTargetData(tps!, cds, extensionAttribSyntax!);
                    TraceLog.LogMessage(
                        $"\t Class declaration syntax {cds} is selected for semantic analysis with attribute {extensionAttribSyntax} and first type parameter {tps!}.  The type expressed is {tps!.Type!}");
                }
            }
            return ret;
        }

        private TypeOfExpressionSyntax? FindFirstAttribArgIfTypeOf(AttributeSyntax extensionAttribSyntax)
        {
            TypeOfExpressionSyntax? ret = null;
            if (extensionAttribSyntax.ArgumentList?.Arguments is { } args && args.Any())
            {
                var first = args.First();
                ret = first.Expression as TypeOfExpressionSyntax;
            }
            return ret;
        }
    }
}