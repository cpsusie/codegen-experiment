using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Cjm.CodeGen.Attributes;
using Cjm.CodeGen.Exceptions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Cjm.CodeGen
{
    internal static class ContextExtensions
    {
        public static void AddOrUpdate<T>(
            this ImmutableSortedDictionary<ClassDeclarationSyntax, ImmutableArray<T>.Builder>.Builder lookupBldr,
            ClassDeclarationSyntax key, T val) 
        {
            if (lookupBldr == null) throw new ArgumentNullException(nameof(lookupBldr));
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (val == null) throw new ArgumentNullException(nameof(val));
            if (!lookupBldr.ContainsKey(key))
            {
                lookupBldr.Add(key, ImmutableArray.CreateBuilder<T>());
            }
            lookupBldr[key].Add(val);
        }

        public static ImmutableSortedDictionary<ClassDeclarationSyntax, ImmutableArray<T>> MakeImmutable<T>(
            this ImmutableSortedDictionary<ClassDeclarationSyntax, ImmutableArray<T>.Builder>.Builder bldr)
        {
            if (bldr == null) throw new ArgumentNullException(nameof(bldr));
            return ImmutableSortedDictionary.CreateRange(bldr.Select(kvp =>
                new KeyValuePair<ClassDeclarationSyntax, ImmutableArray<T>>(kvp.Key,
                    kvp.Value.Count == kvp.Value.Capacity ? kvp.Value.MoveToImmutable() : kvp.Value.ToImmutable())));
        }

        public static SemanticData?
            TryMatchAttribSyntaxAgainstSemanticModelAndExtractInfo<TAttribute>(this in GeneratorExecutionContext context, AttributeSyntax? attribSyntax) where TAttribute : Attribute
        {
            
            if (attribSyntax == null) return null;

            Type attributeType = typeof(TAttribute);
            SemanticModel model;
            INamedTypeSymbol attribTs;
            SymbolInfo si;
            SemanticData? ret = null;
            var token = context.CancellationToken;
            token.ThrowIfCancellationRequested();
            SyntaxTree? tree = attribSyntax.Parent?.SyntaxTree;
            if (tree != null)
            {
                model = context.Compilation.GetSemanticModel(tree, true);
                token.ThrowIfCancellationRequested();
                attribTs = context.Compilation.GetTypeByMetadataName(attributeType.FullName ?? attributeType.Name) ?? throw new CannotFindAttributeSymbolException(attributeType,
                    attributeType.FullName ?? attributeType.Name);
                AttributeArgumentSyntax? firstParam = attribSyntax.ArgumentList?.Arguments.FirstOrDefault();
                EnableAugmentedEnumerationTargetTypeData targetTypeData;
                if (firstParam != null)
                {
                    if (firstParam.Expression is TypeOfExpressionSyntax toes) 
                    {
                        
                        Location l = toes.GetLocation();
                        token.ThrowIfCancellationRequested();
                        TypeInfo ti = model.GetTypeInfo(toes.Type, token);
                        TextSpan ts = toes.Span;
                        (bool isValidTypeKind, TypeKind tk, string reasonWhyNot) = (ti.ConvertedType?.TypeKind) switch
                        {
                            TypeKind.Array => (false, TypeKind.Array, "Arrays are not yet supported."),
                            TypeKind.Class => (true, TypeKind.Class, string.Empty),
                            TypeKind.Struct => (true, TypeKind.Struct, string.Empty),
                            
                            TypeKind.Delegate => GetStdBadTypeMsg(in ts, TypeKind.Delegate ),
                            TypeKind.Dynamic => GetStdBadTypeMsg(in ts, TypeKind.Dynamic ),
                            TypeKind.Enum => GetStdBadTypeMsg(in ts, TypeKind.Enum),
                            TypeKind.Error => GetStdBadTypeMsg(in ts, TypeKind.Error),
                            TypeKind.Interface => GetStdBadTypeMsg(in ts, TypeKind.Interface),
                            TypeKind.Module => GetStdBadTypeMsg(in ts, TypeKind.Module),
                            TypeKind.Pointer => GetStdBadTypeMsg(in ts, TypeKind.Pointer),
                            TypeKind.TypeParameter => GetStdBadTypeMsg(in ts, TypeKind.TypeParameter),
                            TypeKind.Submission => GetStdBadTypeMsg(in ts, TypeKind.Submission),
                            TypeKind.FunctionPointer => GetStdBadTypeMsg(in ts, TypeKind.FunctionPointer),
                            null => (false, TypeKind.Unknown, $"There is no type information available for typeof expression ({toes.Span})."),
                            _ => (false, TypeKind.Unknown, $"Typeof expression ({toes.Span}) target type {ti.ConvertedType.TypeKind} is unknown."),
                        };
                        token.ThrowIfCancellationRequested();
                        if (isValidTypeKind)
                        {
                            targetTypeData = ti.ConvertedType is INamedTypeSymbol targetTypeNts
                                ? EnableAugmentedEnumerationTargetTypeData.CreateSuccessTargetTypeData(firstParam,
                                    l, toes, in ti, tk, targetTypeNts)
                                : EnableAugmentedEnumerationTargetTypeData
                                    .CreateFailureDoesNotSpecifyANamedTypeTargetTypeDataNoTsAvailable(firstParam,
                                        l, toes, in ti, tk);
                        }
                        else
                        {
                            ITypeSymbol? badTs = ti.ConvertedType;
                            targetTypeData = EnableAugmentedEnumerationTargetTypeData.CreateFailureBadTypeKind(firstParam, l, toes, in ti, reasonWhyNot, tk, badTs);
                        }


                        static (bool IsValidTypeKind, TypeKind Tk, string WhyNot) GetStdBadTypeMsg(in TextSpan ts, TypeKind badVal)
                        {
                            const string typeKind = nameof(TypeKind);
                            const string frmtMsg =
                                "Typeof expression ({0})'s {1} evaluates to {2}, which is not a valid type target for the {3} attribute.";
                            return (false, badVal,
                                string.Format(frmtMsg, ts.ToString(), typeKind, badVal,
                                    EnableAugmentedEnumerationExtensionsAttribute.ShortName));
                        }
                      
                    }
                    else
                    {
                        targetTypeData = EnableAugmentedEnumerationTargetTypeData.CreateFailureFirstTypeArgIsNotTypeofExpressionSyntax(firstParam, firstParam.Expression);
                    }

                }
                else
                {
                    targetTypeData = EnableAugmentedEnumerationTargetTypeData.CreateFailureAttributeLacksArgumentList(attribSyntax);
                }
                token.ThrowIfCancellationRequested();
                si = model.GetSymbolInfo(attribSyntax);
                if (si.Symbol is IMethodSymbol { MethodKind: MethodKind.Constructor, ReceiverType: INamedTypeSymbol nts } && SymbolEqualityComparer.Default.Equals(nts, attribTs))
                {
                    var temp = AttribTargetData.CreateTargetData(model, attribTs, si);
                    ret = new SemanticData(in targetTypeData, in temp);
                }
            }
            return ret;

        }
    }
}