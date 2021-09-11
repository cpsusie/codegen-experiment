using System;
using System.Diagnostics;
using System.Threading;
using Cjm.CodeGen.Attributes;
using Cjm.CodeGen.Exceptions;
using HpTimeStamps;
using LoggerLibrary;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonotonicContext = HpTimeStamps.MonotonicStampContext;

namespace Cjm.CodeGen
{
    [Generator]
    public sealed class TransformEnumeratorGenerator : ISourceGenerator, IDisposable
    {
        public event EventHandler<GeneratorTestEnableAugmentSyntaxReceiverPayloadEventArgs>? MatchingSyntaxDetected
        {
            add
            {
                if (!_isDisposed.IsSet)
                {
                    MatchingSyntaxDetectedImpl += value;
                }

                if (_isDisposed.IsSet)
                {
                    MatchingSyntaxDetectedImpl = null;
                }
            }
            remove
            {
                if (!_isDisposed.IsSet)
                {
                    MatchingSyntaxDetectedImpl -= value;
                }

                if (_isDisposed.IsSet)
                {
                    MatchingSyntaxDetectedImpl = null;
                }
            }
        }
            

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
                if (context.SyntaxReceiver is EnableAugmentedEnumerationExtensionSyntaxReceiver rec && rec.FreezeAndQueryHasTargetData(Duration.FromSeconds(2), token))
                {
                    for (int i = 0; i < rec.TargetData.Length; ++i)
                    {
                        ref readonly EnableAugmentedEnumerationExtensionTargetData td =
                            ref rec.TargetData.ItemRef(i);
                        OnMatchingSyntaxReceiver(td);
                        token.ThrowIfCancellationRequested();
                        var results =
                            context
                                .TryMatchAttribSyntaxAgainstSemanticModelAndExtractInfo<
                                    EnableAugmentedEnumerationExtensionsAttribute>(td.AttributeSyntax);
                        if (results != null)
                        {
                            DebugLog.LogMessage($"Results were non-null ({results}).");
                        }
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

        public void Dispose() => Dispose(true);

        private void Dispose(bool disposing)
        {
            if (_isDisposed.TrySet() && disposing)
            {
                _eventPump.Dispose();
            }
            MatchingSyntaxDetectedImpl = null;
        }

        private void OnMatchingSyntaxReceiver(EnableAugmentedEnumerationExtensionTargetData? targetData)
        {
            if (targetData != null)
            {
                _eventPump.RaiseEvent(() =>
                {
                    GeneratorTestEnableAugmentSyntaxReceiverPayloadEventArgs args =
                        new GeneratorTestEnableAugmentSyntaxReceiverPayloadEventArgs(targetData.Value);
                    MatchingSyntaxDetectedImpl?.Invoke(this, args);
                });
            }
        }

        private event EventHandler<GeneratorTestEnableAugmentSyntaxReceiverPayloadEventArgs>?
            MatchingSyntaxDetectedImpl; 

        private LocklessSetOnlyFlag _isDisposed;
        private readonly IEventPump _eventPump = EventPumpFactorySource.FactoryInstance("TransFEnumGen");

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

    internal sealed class LocklessConcreteType
    {


        public Type ConcreteType
        {
            get
            {
                Type? ret = _concreteType;
                if (ret == null)
                {
                    Type theType = InitType();
                    Debug.Assert(theType != null);
                    Interlocked.CompareExchange(ref _concreteType, theType, null);
                    ret = Volatile.Read(ref _concreteType);
                }
                Debug.Assert(ret != null);
                return ret!;
            }
        }

        public string ConcreteTypeName => ConcreteType.Name;

        internal LocklessConcreteType(object owner) => _owner = owner ?? throw new ArgumentNullException(nameof(owner));

        private Type InitType() => _owner.GetType();


        private readonly object _owner;
        private Type? _concreteType;
    }
}
