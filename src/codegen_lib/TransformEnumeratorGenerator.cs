using System;
using System.Threading;
using Cjm.CodeGen.Attributes;
using Cjm.CodeGen.Exceptions;
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
            using var eel = LoggerSource.Logger.CreateEel(nameof(TransformEnumeratorGenerator), nameof(Initialize),
                context.ToString());
            //context.RegisterForSyntaxNotifications(() => new EnableFastLinqClassDeclSyntaxReceiver());
            context.RegisterForSyntaxNotifications(() => new EnableAugmentedEnumerationExtensionSyntaxReceiver());
        }

        /// <inheritdoc />
        public void Execute(GeneratorExecutionContext context)
        {
            using var eel = LoggerSource.Logger.CreateEel(nameof(TransformEnumeratorGenerator), nameof(Execute), context.ToString() ?? "NONE");
            try
            {
                CancellationToken token = context.CancellationToken;
                if (context.SyntaxReceiver is EnableAugmentedEnumerationExtensionSyntaxReceiver
                {
                    HasTargetData: true,
                    TargetData:
                    {
                        ClassToAugment: { } cds, AttributeSyntax: { } attrSynt,
                        AttributeTargetDataSyntax: { } tps
                    }
                })
                {
                    token.ThrowIfCancellationRequested();
                    var results =
                        context
                            .TryMatchAttribSyntaxAgainstSemanticModelAndExtractInfo<
                                EnableAugmentedEnumerationExtensionsAttribute>(attrSynt);
                    if (results != null)
                    {
                        DebugLog.LogMessage("it wasn't null");
                    }
                }



            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                TraceLog.LogException(ex);
                throw;
            }
        }
        

        
    }

    internal static class ContextExtensions
    {
        public static (SemanticModel Model, INamedTypeSymbol AttributeTypeSymbol, SymbolInfo SymbolInfo)?
            TryMatchAttribSyntaxAgainstSemanticModelAndExtractInfo<TAttribute>(this in GeneratorExecutionContext context, AttributeSyntax? attribSyntax) where TAttribute : Attribute
        {
            
            if (attribSyntax == null) return null;

            Type attributeType = typeof(TAttribute);
            SemanticModel model;
            INamedTypeSymbol attribTs;
            SymbolInfo si;
            (SemanticModel Model, INamedTypeSymbol AttributeTypeSymbol, SymbolInfo SymbolInfo)? ret = null;
            var token = context.CancellationToken;
            token.ThrowIfCancellationRequested();
            SyntaxTree? tree = attribSyntax.Parent?.SyntaxTree;
            if (tree != null)
            {
                model = context.Compilation.GetSemanticModel(tree, true);
                token.ThrowIfCancellationRequested();
                attribTs = context.Compilation.GetTypeByMetadataName(attributeType.FullName ?? attributeType.Name) ?? throw new CannotFindAttributeSymbolException(attributeType,
                    attributeType.FullName ?? attributeType.Name);
                token.ThrowIfCancellationRequested();
                si = model.GetSymbolInfo(attribSyntax);
                if (si.Symbol is IMethodSymbol { MethodKind: MethodKind.Constructor, ReceiverType: INamedTypeSymbol nts } && SymbolEqualityComparer.Default.Equals(nts, attribTs))
                {
                    ret = (model, attribTs, si);
                }
            }
            return ret;

        }
    }
}
