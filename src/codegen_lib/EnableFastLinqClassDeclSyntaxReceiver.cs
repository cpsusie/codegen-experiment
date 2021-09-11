using Cjm.CodeGen.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cjm.CodeGen
{
    public sealed class EnableFastLinqClassDeclSyntaxReceiver : CjmAttributeOnClassSyntaxReceiver<EnableFastLinkExtensionsTargetData>
    {
        /// <inheritdoc />
        protected override EnableFastLinkExtensionsTargetData? ExtractTargetDataFromNodeOrNot(SyntaxNode syntax)
        {
            EnableFastLinkExtensionsTargetData? ret = null;
            //using var eel = LoggerSource.Logger.CreateEel(nameof(EnableFastLinqClassDeclSyntaxReceiver),
            //    nameof(OnVisitSyntaxNode), syntax.ToString());
            if (syntax is ClassDeclarationSyntax cds)
            {
                //LoggerSource.Logger.LogMessage($"Examining class declaration syntax: [{cds.ToString()}].");
                bool isPublicStatic = IsPublicStaticPartialClassDeclaration(cds);
                var fastLinkAttributeSyntax = FindExtensionsAttribute(cds, FastLinqExtensionsAttribute.ShortName);
                bool hasFastLinkExtensionAttribute = fastLinkAttributeSyntax != null;
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
                    ret = EnableFastLinkExtensionsTargetData.CreateFastLinkExtensionsTargetData(cds, fastLinkAttributeSyntax!);
                    LoggerSource.Logger.LogMessage(
                        $"\t Class declaration syntax {cds} is selected for semantic analysis with attribute {fastLinkAttributeSyntax}.");
                }
            }
            return ret;
        }

       
    }
}