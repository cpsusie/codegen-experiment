using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cjm.CodeGen.Attributes;
using HpTimeStamps;
using LoggerLibrary;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
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

        public event EventHandler<GeneratorTestEnableAugmentSemanticPayloadEventArgs>? SemanticPayloadFound
        {
            add
            {
                if (!_isDisposed.IsSet)
                {
                    SemanticPayloadFoundImpl += value;
                }

                if (_isDisposed.IsSet)
                {
                    SemanticPayloadFoundImpl = null;
                }
            }
            remove
            {
                if (!_isDisposed.IsSet)
                {
                    SemanticPayloadFoundImpl -= value;
                }

                if (_isDisposed.IsSet)
                {
                    SemanticPayloadFoundImpl = null;
                }
            }
        }

        public event EventHandler<GeneratorTestingEnableAugmentedEnumerationFinalPayloadEventArgs>? FinalPayloadCreated
        {
            add
            {
                if (!_isDisposed.IsSet)
                {
                    FinalPayloadCreatedImpl+= value;
                }

                if (_isDisposed.IsSet)
                {
                    FinalPayloadCreatedImpl = null;
                }
            }
            remove
            {
                if (!_isDisposed.IsSet)
                {
                    FinalPayloadCreatedImpl -= value;
                }

                if (_isDisposed.IsSet)
                {
                    FinalPayloadCreatedImpl= null;
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
                SetUninitNtsIfNot(context);
                CancellationToken token = context.CancellationToken;
                if (context.SyntaxReceiver is EnableAugmentedEnumerationExtensionSyntaxReceiver rec && rec.FreezeAndQueryHasTargetData(Duration.FromSeconds(2), token))
                {
                    ImmutableSortedDictionary<ClassDeclarationSyntax, ImmutableArray<SemanticData>.Builder>.Builder lookup = ClassDeclarationSyntaxExtensions.CreateCdsSortedDicArrayBldr<SemanticData>();
                    for (int i = 0; i < rec.TargetData.Length; ++i)
                    {
                        ref readonly EnableAugmentedEnumerationExtensionTargetData td =
                            ref rec.TargetData.ItemRef(i);
                        OnMatchingSyntaxReceiver(td);
                        token.ThrowIfCancellationRequested();
                        SemanticData? results =
                            context
                                .TryMatchAttribSyntaxAgainstSemanticModelAndExtractInfo<
                                    EnableAugmentedEnumerationExtensionsAttribute>(td.AttributeSyntax);
                        if (results != null)
                        {
                            lookup.AddOrUpdate(td.ClassToAugment, results);
                            OnMatchingSemanticData(results);
                            DebugLog.LogMessage($"Results were non-null ({results}).");
                        }
                    }

                    if (lookup.Values.Any(val => val.Any()))
                    {
                        ImmutableSortedDictionary<ClassDeclarationSyntax, ImmutableHashSet<UsableSemanticData>>
                            useableLookup;
                        ImmutableSortedDictionary<ClassDeclarationSyntax, ImmutableHashSet<GatheredData>>
                            notUseableLookup;
                        {
                            ImmutableSortedDictionary<ClassDeclarationSyntax, ImmutableArray<SemanticData>> immutable = lookup.MakeImmutable();
                            OnFinalPayloadCreated(immutable);
                            (useableLookup, notUseableLookup) = ProcessFinalPayload(immutable, context);
                        }
                        token.ThrowIfCancellationRequested();
                        if (notUseableLookup.Any())
                        {
                            EmitDiagnostics(useableLookup, notUseableLookup, context);
                        }
                        
                        
                    }
                    else
                    {
                        DebugLog.LogMessage($"In lookup with {lookup.Count} class declaration keys, no matching semantic data was found.");
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

        private (ImmutableSortedDictionary<ClassDeclarationSyntax, ImmutableHashSet<UsableSemanticData>>
            useableLookup, ImmutableSortedDictionary<ClassDeclarationSyntax, ImmutableHashSet<GatheredData>>
            notUseableLookup) ProcessFinalPayload(
            ImmutableSortedDictionary<ClassDeclarationSyntax, ImmutableArray<SemanticData>> immutable,
            GeneratorExecutionContext context) 
        {
            CancellationToken token = context.CancellationToken;
            var useable = ImmutableSortedDictionary<ClassDeclarationSyntax, ImmutableHashSet<UsableSemanticData>>.Empty;
            var notUseable = ImmutableSortedDictionary<ClassDeclarationSyntax, ImmutableHashSet<GatheredData>>.Empty;
            try
            {
                ImmutableSortedDictionary<ClassDeclarationSyntax, ImmutableHashSet<GatheredData>> allGatheredData;
                {
                    var bldr =
                        ImmutableSortedDictionary.CreateBuilder<ClassDeclarationSyntax, ImmutableHashSet<GatheredData>>(
                            ClassDeclarationSyntaxExtensions.TheComparer);
                    token.ThrowIfCancellationRequested();
                    foreach (var kvp in immutable)
                    {
                        ClassDeclarationSyntax key = kvp.Key;
                        ImmutableArray<SemanticData> semanticData = kvp.Value;
                        var resultSet = ImmutableHashSet<GatheredData>.Empty;
                        if (semanticData.Any())
                        {
                            context.CancellationToken.ThrowIfCancellationRequested();
                            var hsBldr = ImmutableHashSet.CreateBuilder<GatheredData>();
                            PopulateGatheredData(key, semanticData, hsBldr, context);
                            resultSet = hsBldr.ToImmutable();
                        }

                        bldr.Add(key, resultSet);
                    }
                    token.ThrowIfCancellationRequested();
                    allGatheredData = bldr.ToImmutable();
                }
                if (allGatheredData.Any())
                {
                    (useable, notUseable) = Segregate(allGatheredData, token);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                TraceLog.LogException(ex);
                throw;
            }
            return (useable, notUseable);


        }

        private (ImmutableSortedDictionary<ClassDeclarationSyntax, ImmutableHashSet<UsableSemanticData>> useable, ImmutableSortedDictionary<ClassDeclarationSyntax, ImmutableHashSet<GatheredData>> notUseable) 
            Segregate(ImmutableSortedDictionary<ClassDeclarationSyntax, ImmutableHashSet<GatheredData>> allGatheredData, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        private void PopulateGatheredData(ClassDeclarationSyntax key, ImmutableArray<SemanticData> semanticData, ImmutableHashSet<GatheredData>.Builder gdArrBldr, GeneratorExecutionContext context)
        {
            var token = context.CancellationToken;
            token.ThrowIfCancellationRequested();
            
            foreach (SemanticData sd in semanticData)
            {
                EnumeratorData ed = default;
                INamedTypeSymbol? targetItemType = null;
                ITypeSymbol? enumeratorType = null;
                IMethodSymbol? getEnumeratorMethod = null,
                    enumeratorMoveNextMethod=null,
                    enumeratorResetMethod=null,
                    enumeratorDisposeMethod=null;
                IPropertySymbol? enumeratorCurrentProperty=null;

                INamedTypeSymbol staticClassToAugment = sd.AttributeTargetData.AttributeTypeSymbol;
                INamedTypeSymbol? targetTypeCollection = sd.TargetTypeData.TargetTypeSymbol;
                if (sd.TargetTypeData.IsGoodMatch && targetTypeCollection != null)
                {
                    (getEnumeratorMethod, enumeratorType) =
                        FindGetEnumeratorMethodAndReturnType(targetTypeCollection, token);
                    if (getEnumeratorMethod != null && enumeratorType?.SpecialType != null &&
                        enumeratorType.SpecialType != SpecialType.System_Void && enumeratorType.CanBeReferencedByName && enumeratorType is INamedTypeSymbol
                        {
                            DeclaredAccessibility: Accessibility.Public
                        } namedEts)
                    {
                        bool validTypeKind;
                        (validTypeKind, ed) = ExtractEnumeratorTypeData(ed, namedEts);
                        if (validTypeKind)
                        {
                            (enumeratorCurrentProperty, enumeratorMoveNextMethod, enumeratorDisposeMethod,
                                enumeratorResetMethod) = ExtractEnumeratorPublicMemberDetails(namedEts, token);
                            if (enumeratorCurrentProperty != null)
                            {
                                (bool currentReturnsUsableTypeAndIsNotUnboundGeneric, bool returnTypeIsValueType,
                                        bool propertyOrItsGetterIsReadonly, bool returnsByReference,
                                        bool propertyReturnsByReadonlyReference, INamedTypeSymbol? returnType) =
                                    ExtractCurrentPropertyDetails(enumeratorCurrentProperty, token);
                                targetItemType = returnType;
                                if (currentReturnsUsableTypeAndIsNotUnboundGeneric && targetItemType != null)
                                {
                                    ed = ed.AddPublicCurrentPropertyInfo(returnTypeIsValueType,
                                        propertyOrItsGetterIsReadonly,
                                        !(returnsByReference || propertyReturnsByReadonlyReference),
                                        propertyReturnsByReadonlyReference);
                                }
                            }
                            if (enumeratorMoveNextMethod?.DeclaredAccessibility == Accessibility.Public)
                            {
                                ed = ed.AddPublicMoveNextInfo(true,
                                    enumeratorMoveNextMethod.ReturnType.SpecialType == SpecialType.System_Boolean);
                            }

                            if (enumeratorDisposeMethod?.DeclaredAccessibility == Accessibility.Public)
                            {
                                ed = ed.AddPublicDisposeInfo(true, enumeratorDisposeMethod.ReturnsVoid);
                            }

                            if (enumeratorResetMethod?.DeclaredAccessibility == Accessibility.Public)
                            {
                                ed = ed.AddPublicResetInfo(true, enumeratorResetMethod.ReturnsVoid);
                            }


                            
                            (bool isIEnumeratorT, bool isIEnumerator) = namedEts.SpecialType switch
                            {
                                SpecialType.System_Collections_Generic_IEnumerator_T => (true, true),
                                SpecialType.System_Collections_IEnumerator => (false, true),
                                _ => (false, false)
                            };

                            if (isIEnumeratorT)
                            {
                                ed = EnumeratorData.IsIEnumeratorT;
                            }
                            else if (isIEnumerator)
                            {
                                ed = EnumeratorData.IsNonGenericIEnumerator;
                            }
                            else
                            {
                                (bool implementsIDisposable, bool implementsIEnumerable,
                                    bool implementsGenericIEnumerable) = ed.IsEnumeratorAStackOnlyValueType
                                    ? (false, false, false)
                                    : GatherInterfaceImplementationData(namedEts, token);
                                ed = ed.AddIEnumerableInterfaceImplementationData(
                                    implementsGenericIEnumerable || implementsIEnumerable,
                                    implementsGenericIEnumerable);
                                if (implementsIDisposable && !ed.ImplementsIDisposable)
                                    ed = ed.AddIDisposableImplementationData(true);
                            }
                        }
                    }
                }
                gdArrBldr.Add(new GatheredData(ed, targetItemType, targetTypeCollection, staticClassToAugment, getEnumeratorMethod, enumeratorType, enumeratorCurrentProperty, enumeratorMoveNextMethod, enumeratorResetMethod, enumeratorDisposeMethod));
            }
        }

        private (bool ImplementsIDisposable, bool ImplementsIEnumerator, bool ImplementsGenericIEnumerator) GatherInterfaceImplementationData(INamedTypeSymbol namedEts, CancellationToken token)
        {
            if (namedEts.SpecialType == SpecialType.System_Collections_Generic_IEnumerator_T)
                return (true, true, true);
            if (namedEts.SpecialType == SpecialType.System_Collections_IEnumerator)
                return (true, true, false);
            bool implIDisposable = false;
            bool implIEnumerable = false;
            bool implGenIEn = false;
            foreach (INamedTypeSymbol nts in namedEts.AllInterfaces)
            {
                switch (nts.SpecialType)
                {
                    case SpecialType.System_Collections_Generic_IEnumerator_T:
                        implGenIEn = true;
                        goto case SpecialType.System_Collections_IEnumerator;
                    case SpecialType.System_Collections_IEnumerator:
                        implIEnumerable = true;
                        goto case SpecialType.System_IDisposable;
                    case SpecialType.System_IDisposable:
                        implIDisposable = true;
                        break;
                }

                if (implIDisposable && implIEnumerable && implGenIEn) break;
                token.ThrowIfCancellationRequested();
            }

            return (implIDisposable, implIEnumerable, implGenIEn);
        }

        private (bool ValidUseableNotUnboundGeneric, bool ReturnIsValueType, bool PropertyOrGetterIsReadonly, bool
            GetterReturnsByReference, bool GetterReturnsByReadonlyReference, INamedTypeSymbol? ValidReturnType) ExtractCurrentPropertyDetails(
                IPropertySymbol enumeratorCurrentProperty, CancellationToken token)
        {
            bool returnsValueType = false;
            bool propOrGetterReadonly = false;
            bool getterReturnsByRef = false;
            bool getterReturnsByRoRef = false;


            (INamedTypeSymbol? returnType, bool isValidUseableNotUnbound, IMethodSymbol? getterMethod) =
                GetReturnTypeSymbol(enumeratorCurrentProperty);
            token.ThrowIfCancellationRequested();
            if (isValidUseableNotUnbound && returnType != null && getterMethod != null)
            {
                returnsValueType = returnType.IsValueType;
                propOrGetterReadonly = enumeratorCurrentProperty.IsReadOnly ||
                                       enumeratorCurrentProperty.GetMethod?.IsReadOnly == true;
                getterReturnsByRef = getterMethod.ReturnsByRef || getterMethod.ReturnsByRefReadonly;
                getterReturnsByRoRef = getterMethod.ReturnsByRefReadonly;
            }
            Debug.Assert(returnType != null || !isValidUseableNotUnbound);
            return (isValidUseableNotUnbound, returnsValueType, propOrGetterReadonly, getterReturnsByRef,
                getterReturnsByRoRef, returnType);
            
            

            static (INamedTypeSymbol? GetterReturns, bool ValidUsableNotUnboundGeneric, IMethodSymbol? GetterMethod) GetReturnTypeSymbol(
                IPropertySymbol currentProperty)
            {
                IMethodSymbol? getterMethod = null;
                INamedTypeSymbol? ntsRet = null;
                bool isValid = false;
                if (currentProperty.GetMethod is {ReturnType: INamedTypeSymbol nts } methSymb)
                {
                    getterMethod = methSymb;
                    ntsRet = nts;
                    bool isUnboundGenericType = nts.TypeKind == TypeKind.TypeParameter;
                    isValid = !isUnboundGenericType && nts.CanBeReferencedByName &&
                              nts.DeclaredAccessibility == Accessibility.Public;
                }
                
                Debug.Assert((ntsRet != null && getterMethod != null) || !isValid);
                return (ntsRet, isValid, getterMethod);
            }

        }

        private static (bool ValidTypeKind, EnumeratorData UpdatedEd) ExtractEnumeratorTypeData(EnumeratorData cEd, INamedTypeSymbol enumTs)
        {
            bool validTypeKind = PermittedEnumeratorTypeKinds.Contains(enumTs.TypeKind);
            bool isReferenceType = enumTs.IsReferenceType;
            bool isInterface = isReferenceType && enumTs.TypeKind == TypeKind.Interface;
            bool isClass = validTypeKind && !isInterface && isReferenceType;
            bool isValueType = !isReferenceType && validTypeKind && enumTs.IsValueType;
            bool isReadOnly = isValueType && enumTs.IsReadOnly;
            bool isStackOnly = isValueType && enumTs.IsRefLikeType;
            if (validTypeKind)
            {
                Debug.Assert(isReferenceType || isValueType);
                if (isReferenceType)
                {
                    cEd = cEd.AddEnumeratorTypeInfoForReferenceType(isInterface, isClass);
                }
                else if (isValueType)
                {
                    cEd = cEd.AddEnumeratorTypeInfoForValueType(isReadOnly, isStackOnly);
                }
            }
            else if (isReferenceType)
            {
                cEd = cEd.AddEnumeratorTypeInfoForReferenceType(false, false);
            }

            return (validTypeKind, cEd);
        }

        private static (IPropertySymbol? EnumeratorCurrentProperty, IMethodSymbol? EnumeratorMoveNextMethod, IMethodSymbol? EnumeratorDisposeMethod, IMethodSymbol? EnumeratorResetMethod) 
            ExtractEnumeratorPublicMemberDetails(INamedTypeSymbol namedEts, CancellationToken token)
        {
            try
            {
                IPropertySymbol? currentProperty = FindPublicPropertyWithPublicGetterMatchingName(namedEts, CurrentPropertyName, token);
                IMethodSymbol? moveNext = FindPublicMethodMatchingNameWithZeroParams(namedEts, MoveNextMethodName, token);
                IMethodSymbol? reset = FindPublicMethodMatchingNameWithZeroParams(namedEts, ResetMethodName, token);
                IMethodSymbol? dispose = FindPublicMethodMatchingNameWithZeroParams(namedEts, DisposeMethodName, token);
                return (currentProperty, moveNext, reset, dispose);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                TraceLog.LogError(
                    $"Unexpected exception thrown in {nameof(ExtractEnumeratorPublicMemberDetails)} method for type symbol {namedEts.Name}.  Exception: \"{ex}\".");
                throw;
            }

            
        }

        public (IMethodSymbol? GetEnumeratorMethodSymbol, ITypeSymbol? EnumeratorType)
            FindGetEnumeratorMethodAndReturnType(INamedTypeSymbol searchMe, CancellationToken token)
        {
            IMethodSymbol? getEnumeratorMethod;
            ITypeSymbol? returnType;
            try
            {
                getEnumeratorMethod =
                    FindPublicMethodMatchingNameWithZeroParams(searchMe, GetEnumeratorMethodName, token);
                returnType = getEnumeratorMethod?.ReturnType;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                TraceLog.LogError(
                    $"Unexpected exception thrown in {nameof(FindGetEnumeratorMethodAndReturnType)} method for type symbol {searchMe.Name}.  Exception: \"{ex}\".");
                throw;
            }
            Debug.Assert(getEnumeratorMethod != null || returnType == null);
            return (getEnumeratorMethod, returnType);
        }


        private static Task EmitDiagnostics(ImmutableSortedDictionary<ClassDeclarationSyntax, ImmutableHashSet<UsableSemanticData>> useableLookup,
            ImmutableSortedDictionary<ClassDeclarationSyntax, ImmutableHashSet<GatheredData>> notUseableLookup, GeneratorExecutionContext context)
        {
            try
            {
                var token = context.CancellationToken;
                if (!notUseableLookup.Any())
                {
                    DebugLog.LogError("No non-useable items to emit diagnostics for.");
                }
                token.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                TraceLog.LogException(ex);
                throw;
            }
            return Task.CompletedTask;
        }

        private void SetUninitNtsIfNot(GeneratorExecutionContext context)
        {
            using var eel = TraceLog.CreateEel(nameof(TransformEnumeratorGenerator), nameof(SetUninitNtsIfNot),
                context.Compilation.ToString());
            Type unitType = typeof(UninitializedTargetData);
            INamedTypeSymbol ts =
                context.Compilation.GetTypeByMetadataName(unitType.FullName ??
                                                          unitType.AssemblyQualifiedName ?? unitType.Name) ??
                throw new InvalidOperationException($"Unable to find named type symbol for {unitType}.");
            IPropertySymbol ps = ts.GetMembers().OfType<IPropertySymbol>().First() ??
                                 throw new InvalidOperationException("Cannot find property symbol.");
            IMethodSymbol ns = ts.GetMembers().OfType<IMethodSymbol>().Last() ??
                               throw new InvalidOperationException("Cannot find method symbol.");
            GenerationData.EnsureSet(ts, ps, ns);
        }


        public void Dispose() => Dispose(true);

        
        private void Dispose(bool disposing)
        {
            if (_isDisposed.TrySet() && disposing)
            {
                _eventPump.Dispose();
            }
            MatchingSyntaxDetectedImpl = null;
            SemanticPayloadFoundImpl = null;
            FinalPayloadCreatedImpl = null;
        }

        private void OnMatchingSyntaxReceiver(EnableAugmentedEnumerationExtensionTargetData? targetData)
        {
            if (targetData != null)
            {
                _eventPump.RaiseEvent(() =>
                {
                    GeneratorTestEnableAugmentSyntaxReceiverPayloadEventArgs args =
                        new(targetData.Value);
                    MatchingSyntaxDetectedImpl?.Invoke(this, args);
                });
            }
        }

        private void OnMatchingSemanticData(SemanticData? results)
        {
            if (results != null)
            {
                _eventPump.RaiseEvent(() =>
                {
                    GeneratorTestEnableAugmentSemanticPayloadEventArgs args =
                        new(results);
                    SemanticPayloadFoundImpl?.Invoke(this, args);
                });
            }
        }

        private void OnFinalPayloadCreated(
            ImmutableSortedDictionary<ClassDeclarationSyntax, ImmutableArray<SemanticData>>? lookup)
        {
            if (lookup != null)
            {
                _eventPump.RaiseEvent(() =>
                {
                    GeneratorTestingEnableAugmentedEnumerationFinalPayloadEventArgs args =
                        new(lookup);
                    FinalPayloadCreatedImpl?.Invoke(this, args);
                });
            }
        }

        private static IMethodSymbol?
            FindPublicMethodMatchingNameWithZeroParams(ITypeSymbol searchMe, string methodName) =>
            FindPublicMethodMatchingNameAndNumParams(searchMe, methodName, CancellationToken.None, 0);

        private static IMethodSymbol? FindPublicMethodMatchingNameAndNumParams(ITypeSymbol searchMe, string methodName,
            int numParams) => FindPublicMethodMatchingNameAndNumParams(searchMe, methodName, CancellationToken.None, numParams);

        private static IMethodSymbol? FindPublicMethodMatchingNameWithZeroParams(ITypeSymbol searchMe, string methodName,
            CancellationToken token) => FindPublicMethodMatchingNameAndNumParams(searchMe, methodName, token, 0);

        private static IMethodSymbol? FindPublicMethodMatchingNameAndNumParams(ITypeSymbol searchMe, string methodName, CancellationToken token, int numParams)
        {
            
            token.ThrowIfCancellationRequested();
            return (from symbol in searchMe.GetMembers(methodName)
                let methodSymbol = symbol as IMethodSymbol
                where methodSymbol is
                {
                    CanBeReferencedByName: true, DeclaredAccessibility: Accessibility.Public, Parameters: var paramArray
                } && paramArray.Length == numParams
                select methodSymbol).FirstOrDefault();
        }

        private static IPropertySymbol? FindPublicPropertyWithPublicGetterMatchingName(ITypeSymbol searchMe,
            string propertyName,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return (from propertySymbol in searchMe.GetMembers(propertyName).OfType<IPropertySymbol>()
                where propertySymbol is { CanBeReferencedByName: true, DeclaredAccessibility: Accessibility.Public }
                let getterMethod = propertySymbol.GetMethod
                where getterMethod?.DeclaredAccessibility == Accessibility.Public || getterMethod?.DeclaredAccessibility == Accessibility.NotApplicable
                select propertySymbol).FirstOrDefault();
        }


        private static readonly ImmutableArray<TypeKind> PermittedEnumeratorTypeKinds =
            ImmutableArray.Create(TypeKind.Class, TypeKind.Struct, TypeKind.Interface);
        private const string GetEnumeratorMethodName = "GetEnumerator";
        private const string CurrentPropertyName = "Current";
        private const string MoveNextMethodName = "MoveNext";
        private const string ResetMethodName = "Reset";
        private const string DisposeMethodName = "Dispose";
        private event EventHandler<GeneratorTestingEnableAugmentedEnumerationFinalPayloadEventArgs>? FinalPayloadCreatedImpl;
        private event EventHandler<GeneratorTestEnableAugmentSyntaxReceiverPayloadEventArgs>?
            MatchingSyntaxDetectedImpl;
        private event EventHandler<GeneratorTestEnableAugmentSemanticPayloadEventArgs>? SemanticPayloadFoundImpl;
        private LocklessSetOnlyFlag _isDisposed;
        private readonly IEventPump _eventPump = EventPumpFactorySource.FactoryInstance("TransFEnumGen");

    }
}
