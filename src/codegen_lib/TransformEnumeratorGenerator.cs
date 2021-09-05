using System;
using System.Collections.Generic;
using System.Threading;
using Cjm.CodeGen.Attributes;
using Cjm.CodeGen.Exceptions;
using LoggerLibrary;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TypeArgumentListSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeArgumentListSyntax;

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
            context.RegisterForSyntaxNotifications(() => new EnableFastLinqClassDeclSyntaxReceiver());
            context.RegisterForSyntaxNotifications(() => new EnableAugmentedEnumerationExtensionSyntaxReceiver());
        }

        /// <inheritdoc />
        public void Execute(GeneratorExecutionContext context)
        {
            using var eel = LoggerSource.Logger.CreateEel(nameof(TransformEnumeratorGenerator), nameof(Execute), context.ToString() ?? "NONE");
            try
            {
                CancellationToken token = context.CancellationToken;
                if (context.SyntaxReceiver is EnableFastLinqClassDeclSyntaxReceiver { TargetData: { ClassToAugment: {} augmentSyntax, AttributeSyntax: {} attribSyntax } })
                {
//#if DEBUG
//                    Debugger.Launch();
//#endif
                    token.ThrowIfCancellationRequested();
                    LoggerSource.Logger.LogMessage($"Examining attribute syntax [{attribSyntax.ToString()}] from type decl syntax: [{augmentSyntax.ToString()}]" );
                    var tree = attribSyntax.Parent?.SyntaxTree;
                    if (tree != null)
                    {
                        var model = context.Compilation.GetSemanticModel(tree, true);
                        INamedTypeSymbol? fastLinqEnableAttribute =
                            context.Compilation.GetTypeByMetadataName(typeof(FastLinqExtensionsAttribute).FullName);
                        if (fastLinqEnableAttribute == null)
                        {
                            throw new CannotFindAttributeSymbolException(typeof(FastLinqExtensionsAttribute),
                                typeof(FastLinqExtensionsAttribute).FullName);
                        }
                        SymbolInfo symbolInfo = model.GetSymbolInfo(attribSyntax);
                        LoggerSource.Logger.LogMessage($"Examining symbol info ({symbolInfo}.");
                        if (symbolInfo.Symbol is IMethodSymbol ms && ms.MethodKind == MethodKind.Constructor && ms.ReceiverType is INamedTypeSymbol nts && SymbolEqualityComparer.Default.Equals(nts, fastLinqEnableAttribute))
                        {
                            LoggerSource.Logger.LogMessage($"The type {augmentSyntax.Identifier} is decorated with the {nts.Name} attribute.");
                        }
                        else
                        {
                            LoggerSource.Logger.LogMessage("Couldn't find attribute symbol.");
                        }
                    }
                    else
                    {
                        LoggerSource.Logger.LogMessage($"{attribSyntax} has a null parent tree.");
                    }
                }
                else if (context.SyntaxReceiver is EnableAugmentedEnumerationExtensionSyntaxReceiver
                {
                    HasTargetData: true,
                    TargetData:
                    {
                        ClassToAugment: { } cds, AttributeSyntax: { } attrSynt,
                        AugmentedClassFirstTypeParameter: { } tps
                    }
                })
                {
                    token.ThrowIfCancellationRequested();

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

    public abstract class CjmAttributeOnClassSyntaxReceiver<TTargetData> : ISyntaxReceiver where TTargetData : struct, ITargetData, IEquatable<TTargetData>
    {
        public bool HasTargetData => _alreadySet.IsSet && TargetData.HasValue;
        public TTargetData? TargetData => _targetData;
        protected bool AlreadySet => _alreadySet.IsSet;

        public void OnVisitSyntaxNode(SyntaxNode visited) =>
        
            SetTargetData(ExtractTargetDataFromNodeOrNot(visited));
        

        protected abstract TTargetData? ExtractTargetDataFromNodeOrNot(SyntaxNode visited);

        private void SetTargetData(in TTargetData? setMe)
        {
            _alreadySet.SetOrThrow();
            _targetData = setMe;
        }

        protected static bool IsPublicStaticClassDeclaration(ClassDeclarationSyntax cds)
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

        protected static AttributeSyntax? FindFastLinkExtensionsAttribute(ClassDeclarationSyntax cds, string attributeShortName)
        {
            foreach (var attribList in cds.AttributeLists)
            {
                foreach (var attrib in attribList.Attributes)
                {

                    if (attrib.Name.ToString() == attributeShortName)
                        return attrib;
                }
            }
            return null;
        }

        private LocklessSetOnlyFlag _alreadySet;
        private TTargetData? _targetData;
    }

    public interface ITargetData
    {
        ClassDeclarationSyntax ClassToAugment { get; }
        AttributeSyntax AttributeSyntax { get; }
    }

    public readonly struct EnableAugmentedEnumerationExtensionTargetData : ITargetData,
        IEquatable<EnableAugmentedEnumerationExtensionTargetData>
    {
        public static EnableAugmentedEnumerationExtensionTargetData CreateTargetData(TypeParameterSyntax tps,
            ClassDeclarationSyntax cds, AttributeSyntax ats) => new(
            tps ?? throw new ArgumentNullException(nameof(tps)), cds ?? throw new ArgumentNullException(nameof(cds)),
            ats ?? throw new ArgumentNullException(nameof(ats)));

        public TypeParameterSyntax AugmentedClassFirstTypeParameter { get; }
        public ClassDeclarationSyntax ClassToAugment => _base.ClassToAugment;
        public AttributeSyntax AttributeSyntax => _base.AttributeSyntax;

        private EnableAugmentedEnumerationExtensionTargetData(TypeParameterSyntax tps,
            ClassDeclarationSyntax cds, AttributeSyntax ats)
        {
            _base = EnableFastLinkExtensionsTargetData.CreateFastLinkExtensionsTargetData(cds, ats);
            AugmentedClassFirstTypeParameter = tps;
        }

        public static bool operator ==(in EnableAugmentedEnumerationExtensionTargetData lhs,
            in EnableAugmentedEnumerationExtensionTargetData rhs) => lhs._base == rhs._base &&
                                                                     TheTpsComparer.Equals(
                                                                         lhs.AugmentedClassFirstTypeParameter,
                                                                         rhs.AugmentedClassFirstTypeParameter);

        public static bool operator !=(in EnableAugmentedEnumerationExtensionTargetData lhs,
            in EnableAugmentedEnumerationExtensionTargetData rhs) => !(lhs == rhs);

        public override int GetHashCode()
        {
            int hash = TheTpsComparer.GetHashCode(AugmentedClassFirstTypeParameter);
            unchecked
            {
                hash = (hash * 397) ^ _base.GetHashCode();
            }
            return hash;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) =>
            obj is EnableAugmentedEnumerationExtensionTargetData eatd && eatd == this;
        public bool Equals(EnableAugmentedEnumerationExtensionTargetData other) => other == this;

        /// <inheritdoc />
        public override string ToString() =>
            $"{nameof(EnableAugmentedEnumerationExtensionTargetData)} -- {nameof(ClassToAugment)}: {ClassToAugment.Identifier.Text}; " +
            $"{nameof(AttributeSyntax)}: {AttributeSyntax.Name}; {nameof(AugmentedClassFirstTypeParameter)}: " +
            $"{AugmentedClassFirstTypeParameter.Identifier.Text}.";
        

        private readonly EnableFastLinkExtensionsTargetData _base;
        private static readonly EqualityComparer<TypeParameterSyntax> TheTpsComparer = EqualityComparer<TypeParameterSyntax>.Default;
    }

    public readonly struct EnableFastLinkExtensionsTargetData : ITargetData,
        IEquatable<EnableFastLinkExtensionsTargetData>
    {
        public static EnableFastLinkExtensionsTargetData
            CreateFastLinkExtensionsTargetData(ClassDeclarationSyntax cds, AttributeSyntax ats) =>
            new EnableFastLinkExtensionsTargetData(cds ?? throw new ArgumentNullException(nameof(cds)),
                ats ?? throw new ArgumentNullException(nameof(ats)));

        public ClassDeclarationSyntax ClassToAugment { get; }
        public AttributeSyntax AttributeSyntax { get; }
        
        private EnableFastLinkExtensionsTargetData(ClassDeclarationSyntax cds, AttributeSyntax ats)
        {
            ClassToAugment = cds;
            AttributeSyntax = ats;
        }

        public static bool operator
            ==(EnableFastLinkExtensionsTargetData lhs, EnableFastLinkExtensionsTargetData rhs) =>
            TheCdsComp.Equals(lhs.ClassToAugment, rhs.ClassToAugment) &&
            TheAttComp.Equals(lhs.AttributeSyntax, rhs.AttributeSyntax);

        public static bool operator
            !=(EnableFastLinkExtensionsTargetData lhs, EnableFastLinkExtensionsTargetData rhs) => !(lhs == rhs);

        public override int GetHashCode()
        {
            int hash = TheCdsComp.GetHashCode(ClassToAugment);
            unchecked
            {
                hash = (hash * 397) ^ TheAttComp.GetHashCode(AttributeSyntax);
            }

            return hash;
        }
        public override bool Equals(object other) => other is EnableFastLinkExtensionsTargetData efetg && efetg == this;

        public bool Equals(EnableFastLinkExtensionsTargetData other) => other == this;

        /// <inheritdoc />
        public override string ToString() =>
            $"{nameof(EnableFastLinkExtensionsTargetData)} -- {nameof(ClassToAugment)}: {ClassToAugment.Identifier.Text}; {nameof(AttributeSyntax)}: {AttributeSyntax.Name}.";
        

        private static readonly EqualityComparer<AttributeSyntax> TheAttComp = EqualityComparer<AttributeSyntax>.Default;
        private static readonly EqualityComparer<ClassDeclarationSyntax> TheCdsComp = EqualityComparer<ClassDeclarationSyntax>.Default;
    }

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
                bool isPublicStatic = IsPublicStaticClassDeclaration(cds);
                var fastLinkAttributeSyntax = FindFastLinkExtensionsAttribute(cds, FastLinqExtensionsAttribute.ShortName);
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
                bool isPublicStatic = IsPublicStaticClassDeclaration(cds);
                var fastLinkAttributeSyntax = FindFastLinkExtensionsAttribute(cds, EnableAugmentedEnumerationExtensionsAttribute.ShortName);
                bool hasFastLinkExtensionAttribute = fastLinkAttributeSyntax != null;
                TypeParameterSyntax? tps = hasFastLinkExtensionAttribute ? FindFirstTypeParameterOnDecoratedClass(cds) : null;
                bool hasTps = tps != null;
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
                if (isPublicStatic && hasFastLinkExtensionAttribute && hasTps)
                {
                    ret = EnableAugmentedEnumerationExtensionTargetData.CreateTargetData(tps!, cds, fastLinkAttributeSyntax!);
                    LoggerSource.Logger.LogMessage(
                        $"\t Class declaration syntax {cds} is selected for semantic analysis with attribute {fastLinkAttributeSyntax} and first type parameter {tps}.");
                }
            }
            return ret;
        }

        private TypeParameterSyntax? FindFirstTypeParameterOnDecoratedClass(ClassDeclarationSyntax cds)
            => cds.TypeParameterList?.Parameters.FirstOrDefault();
           
        
    }

    internal static class LoggerSource
    {
        public static readonly ICodeGenLogger Logger = CodeGenLogger.Logger;
    }
}
