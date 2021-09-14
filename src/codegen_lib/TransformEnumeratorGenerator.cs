using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cjm.CodeGen.Attributes;
using Cjm.CodeGen.Exceptions;
using HpTimeStamps;
using LoggerLibrary;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using AttributeSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax;
using ExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax;
using MonotonicContext = HpTimeStamps.MonotonicStampContext;
using TypeOfExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.TypeOfExpressionSyntax;

namespace Cjm.CodeGen
{
    
    
    internal sealed record GatheredData(EnumeratorData? Ed, INamedTypeSymbol? TargetItemType,
        INamedTypeSymbol? TargetTypeCollection, INamedTypeSymbol? StaticClassToAugment,
        IMethodSymbol? GetEnumeratorMethod, ITypeSymbol? EnumeratorType, IPropertySymbol? EnumeratorCurrentProperty,
        IMethodSymbol? EnumeratorMoveNextMethod, IMethodSymbol? EnumeratorResetMethod,
        IMethodSymbol? EnumeratorDisposeMethod);

    

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
                        ImmutableSortedDictionary<ClassDeclarationSyntax, ImmutableArray<UsableSemanticData>>
                            useableLookup;
                        ImmutableSortedDictionary<ClassDeclarationSyntax, ImmutableArray<GatheredData>>
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

        private (ImmutableSortedDictionary<ClassDeclarationSyntax, ImmutableArray<UsableSemanticData>>
            useableLookup, ImmutableSortedDictionary<ClassDeclarationSyntax, ImmutableArray<GatheredData>>
            notUseableLookup) ProcessFinalPayload(
            ImmutableSortedDictionary<ClassDeclarationSyntax, ImmutableArray<SemanticData>> immutable,
            GeneratorExecutionContext context) 
        {
            CancellationToken token = context.CancellationToken;
            try
            {
                var bldr =
                    ImmutableSortedDictionary.CreateBuilder<ClassDeclarationSyntax, ImmutableArray<GatheredData>>(
                        ClassDeclarationSyntaxExtensions.TheComparer);
                token.ThrowIfCancellationRequested();
                foreach (var kvp in immutable)
                {
                    ClassDeclarationSyntax key = kvp.Key;
                    ImmutableArray<SemanticData> semanticData = kvp.Value;
                    var resultArray = ImmutableArray<GatheredData>.Empty;
                    if (semanticData.Any())
                    {
                        var gdArrBldr = ImmutableArray.CreateBuilder<GatheredData>(semanticData.Length);
                        PopulateGatheredData(key, semanticData, gdArrBldr, context);
                    }
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

            return (ImmutableSortedDictionary<ClassDeclarationSyntax, ImmutableArray<UsableSemanticData>>.Empty,
                    ImmutableSortedDictionary<ClassDeclarationSyntax, ImmutableArray<GatheredData>>.Empty)
                ;
        }

        private void PopulateGatheredData(ClassDeclarationSyntax key, ImmutableArray<SemanticData> semanticData, ImmutableArray<GatheredData>.Builder gdArrBldr, GeneratorExecutionContext context)
        {
            var token = context.CancellationToken;
            token.ThrowIfCancellationRequested();
            Debug.Assert(semanticData.Length > 0 && gdArrBldr.Capacity == semanticData.Length);
            foreach (SemanticData sd in semanticData)
            {
                EnumeratorData ed = default;
                INamedTypeSymbol? targetItemType=null, targetTypeCollection=null, staticClassToAugment=null;
                ITypeSymbol? enumeratorType = null;
                IMethodSymbol? getEnumeratorMethod = null,
                    enumeratorMoveNextMethod=null,
                    enumeratorResetMethod=null,
                    enumeratorDisposeMethod=null;
                IPropertySymbol? enumeratorCurrentProperty=null;

                staticClassToAugment = sd.AttributeTargetData.AttributeTypeSymbol;
                targetTypeCollection = sd.TargetTypeData.TargetTypeSymbol;
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
                                enumeratorMoveNextMethod) = ExtractEnumeratorPublicMemberDetails(namedEts, token);
                        }
                    }
                }


                gdArrBldr.Add(new GatheredData(ed, targetItemType, targetTypeCollection, staticClassToAugment, getEnumeratorMethod, enumeratorType, enumeratorCurrentProperty, enumeratorMoveNextMethod, enumeratorResetMethod, enumeratorDisposeMethod));
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
            IPropertySymbol? currentProperty = null;
            IMethodSymbol? moveNext = null;
            IMethodSymbol? reset = null;
            IMethodSymbol? dispose = null;
            try
            {
                token.ThrowIfCancellationRequested();
                //todo fixit resume here
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

            return (currentProperty, moveNext, reset, dispose);
        }

        public (IMethodSymbol? GetEnumeratorMethodSymbol, ITypeSymbol? EnumeratorType)
            FindGetEnumeratorMethodAndReturnType(INamedTypeSymbol searchMe, CancellationToken token)
        {
            IMethodSymbol? getEnumeratorMethod;
            ITypeSymbol? returnType;
            try
            {
                token.ThrowIfCancellationRequested();
                getEnumeratorMethod = (from symbol in searchMe.GetMembers(GetEnumeratorMethodName)
                    let methodSymbol = symbol as IMethodSymbol
                    where methodSymbol is { CanBeReferencedByName: true, DeclaredAccessibility: Accessibility.Public }
                    select methodSymbol).FirstOrDefault();
                token.ThrowIfCancellationRequested();
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


        private static Task EmitDiagnostics(ImmutableSortedDictionary<ClassDeclarationSyntax, ImmutableArray<UsableSemanticData>> useableLookup, ImmutableSortedDictionary<ClassDeclarationSyntax, ImmutableArray<GatheredData>> notUseableLookup, GeneratorExecutionContext context)
        {
            try
            {
                var token = context.CancellationToken;
                if (!notUseableLookup.Any())
                {
                    DebugLog.LogError("No non-useable items to emit diagnostics for.");
                }
            }
            catch (OperationCanceledException)
            {
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
            IMethodSymbol ns = ts.GetMembers().OfType<IMethodSymbol>().First() ??
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


        private static readonly ImmutableArray<TypeKind> PermittedEnumeratorTypeKinds =
            ImmutableArray.Create(TypeKind.Class, TypeKind.Struct, TypeKind.Interface);
        private const string GetEnumeratorMethodName = "GetEnumerator";
        private event EventHandler<GeneratorTestingEnableAugmentedEnumerationFinalPayloadEventArgs>? FinalPayloadCreatedImpl;
        private event EventHandler<GeneratorTestEnableAugmentSyntaxReceiverPayloadEventArgs>?
            MatchingSyntaxDetectedImpl;
        private event EventHandler<GeneratorTestEnableAugmentSemanticPayloadEventArgs>? SemanticPayloadFoundImpl;
        private LocklessSetOnlyFlag _isDisposed;
        private readonly IEventPump _eventPump = EventPumpFactorySource.FactoryInstance("TransFEnumGen");

    }

    public sealed class UsableSemanticData : IEquatable<UsableSemanticData?>
    {
        
        public SemanticData SemanticInfo => _semanticData;
        public ref readonly GenerationData GenerationInfo => ref _generationData;

        private UsableSemanticData(SemanticData sd, in GenerationData gd)
        {
            _generationData = gd.IsInvalidDefault
                ? throw new ArgumentException("Parameter was invalid, uninitialized default value.", nameof(gd))
                : gd;
            _semanticData = sd ?? throw new ArgumentNullException(nameof(sd));
            _stringRep = new LocklessLazyWriteOnce<string>(GetStringRep);
        }

        public bool Equals(UsableSemanticData? other) =>
            other?._semanticData == _semanticData && other._generationData == _generationData;

        public override int GetHashCode()
        {
            int hash = _semanticData.GetHashCode();
            unchecked
            {
                hash = (hash * 397) ^ _generationData.GetHashCode();
            }
            return hash;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => Equals(obj as UsableSemanticData);
        public static bool operator ==(UsableSemanticData lhs, UsableSemanticData rhs) =>
            ReferenceEquals(lhs, rhs) || lhs?.Equals(rhs) == true;
        public static bool operator !=(UsableSemanticData lhs, UsableSemanticData rhs) => !(lhs == rhs);

        /// <inheritdoc />
        public override string ToString() => _stringRep.Value;
           
        private string GetStringRep() =>
            $"{nameof(UsableSemanticData)} -- Semantic Data: {_semanticData.ToString()} \tGenerationData: {_generationData.ToString()}";
        private readonly LocklessLazyWriteOnce<string> _stringRep;
        private readonly GenerationData _generationData;
        private readonly SemanticData _semanticData;
    }

    public readonly struct GenerationData : IEquatable<GenerationData>, IHasGenericByRefRoEqComparer<GenerationData.EqComp, GenerationData>
    {
        public static readonly GenerationData InvalidDefault = default;

        private GenerationData(EnumeratorData ed, INamedTypeSymbol targetItemType,
            INamedTypeSymbol? targetTypeCollection, INamedTypeSymbol staticClassToAugment,
            IMethodSymbol getEnumeratorMethod, ITypeSymbol enumeratorType, IPropertySymbol enumeratorCurrentProperty,
            IMethodSymbol enumeratorMoveNextMethod, IMethodSymbol enumeratorResetMethod,
            IMethodSymbol enumeratorDisposeMethod)
        {
            _ed = ed;
            _targetItemType = targetItemType;
            _targetTypeCollection = targetTypeCollection;
            _staticClassToAugment = staticClassToAugment;
            _getEnumeratorMethod = getEnumeratorMethod;
            _enumeratorType = enumeratorType;
            _enumeratorCurrentProperty = enumeratorCurrentProperty;
            _enumeratorMoveNextMethod = enumeratorMoveNextMethod;
            EnumeratorResetMethod = enumeratorResetMethod;
            EnumeratorDisposeMethod = enumeratorDisposeMethod;
        }

        internal static bool IsDefaultSet => TheUninitializedNamedTypeSymbol.IsSet;
        private static INamedTypeSymbol DefaultNts => TheUninitializedNamedTypeSymbol.Value.NamedType;
        private static IPropertySymbol DefaultPs => TheUninitializedNamedTypeSymbol.Value.PropertySymbol;
        private static IMethodSymbol DefaultMs => TheUninitializedNamedTypeSymbol.Value.MethodSymbol;

        public EnumeratorData EnumeratorData => _ed;
        public INamedTypeSymbol TargetItemType => _targetItemType ?? DefaultNts;
        public INamedTypeSymbol TargetCollectionType => _targetTypeCollection ?? DefaultNts;
        public INamedTypeSymbol StaticClassToAugment => _staticClassToAugment ?? DefaultNts;
        public bool IsInvalidDefault => this == InvalidDefault;

        public IMethodSymbol GetEnumeratorMethod => _getEnumeratorMethod ?? DefaultMs;

        public ITypeSymbol EnumeratorType => _enumeratorType ?? DefaultNts;

        public IPropertySymbol EnumeratorCurrentProperty => _enumeratorCurrentProperty ?? DefaultPs;
        public IMethodSymbol EnumeratorMoveNextMethod => _enumeratorMoveNextMethod ?? DefaultMs;
        public IMethodSymbol? EnumeratorResetMethod { get; }

        public IMethodSymbol? EnumeratorDisposeMethod { get; }


        internal static void EnsureSet(INamedTypeSymbol
            nts, IPropertySymbol ps, IMethodSymbol ms)
        {
            if (TheUninitializedNamedTypeSymbol.IsSet) return;

            UninitializedSymbols symbols = new (nts ?? throw new ArgumentNullException(nameof(nts)),
                ps ?? throw new ArgumentNullException(nameof(ps)), ms ?? throw new ArgumentNullException(nameof(ms)));

            bool setIt = TheUninitializedNamedTypeSymbol.TrySet(symbols);
            if (!setIt && !TheUninitializedNamedTypeSymbol.IsSet)
                throw new InvalidOperationException("Unable to set initial value.");
        }

        static GenerationData()
        {
            TheUninitializedNamedTypeSymbol = new LocklessWriteOnce<UninitializedSymbols>();
        }

        public override int GetHashCode()
        {
            int hash = SymbolEqualityComparer.IncludeNullability.GetHashCode(TargetItemType);
            unchecked
            {
                hash = (hash * 397) ^ SymbolEqualityComparer.Default.GetHashCode(TargetCollectionType);
                hash = (hash * 397) ^ SymbolEqualityComparer.Default.GetHashCode(StaticClassToAugment);
                hash = (hash * 397) ^ _ed.GetHashCode();
            }
            return hash;
        }

        public static bool operator ==(in GenerationData lhs, in GenerationData rhs) => lhs._ed == rhs._ed &&
            SymbolEqualityComparer.IncludeNullability.Equals(lhs.TargetItemType, rhs.TargetItemType) &&
            SymbolEqualityComparer.Default.Equals(lhs.TargetCollectionType, rhs.TargetCollectionType) &&
            SymbolEqualityComparer.Default.Equals(lhs.StaticClassToAugment, rhs.StaticClassToAugment) &&
            SymbolEqualityComparer.IncludeNullability.Equals(lhs.EnumeratorCurrentProperty,
                rhs.EnumeratorCurrentProperty) &&
            SymbolEqualityComparer.Default.Equals(lhs.GetEnumeratorMethod, rhs.GetEnumeratorMethod) &&
            SymbolEqualityComparer.Default.Equals(lhs.EnumeratorMoveNextMethod, rhs.EnumeratorMoveNextMethod) &&
            SymbolEqualityComparer.Default.Equals(lhs.EnumeratorDisposeMethod, rhs.EnumeratorDisposeMethod) &&
            SymbolEqualityComparer.IncludeNullability.Equals(lhs.EnumeratorType, rhs.EnumeratorType) &&
            SymbolEqualityComparer.Default.Equals(lhs.EnumeratorResetMethod, rhs.EnumeratorResetMethod);

        public static bool operator !=(in GenerationData lhs, in GenerationData rhs) => !(lhs == rhs);

        public bool Equals(GenerationData other) => other == this;

        /// <inheritdoc />
        public EqComp GetComparer() => default;
        

        public override bool Equals(object? other) => other is GenerationData gd && gd == this;

        /// <inheritdoc />
        public override string ToString() =>
            $"GenerationData -- {nameof(TargetItemType)}: \t{TargetItemType.ToDisplayString()}; \t{nameof(TargetCollectionType)}: " +
            $"\t{TargetCollectionType.ToDisplayString()}; \t{nameof(StaticClassToAugment)}: \t{StaticClassToAugment.ToDisplayString()};" +
            $" \t{nameof(EnumeratorData)}: \t{EnumeratorData.ToString()}.";

        public readonly struct EqComp : IByRoRefEqualityComparer<GenerationData>
        {
            public bool Equals(in GenerationData l, in GenerationData r) => l == r;
            public int GetHashCode(in GenerationData gd) => gd.GetHashCode();

        }

        private readonly EnumeratorData _ed;
        private readonly INamedTypeSymbol? _targetItemType;
        private readonly INamedTypeSymbol? _targetTypeCollection;
        private readonly INamedTypeSymbol? _staticClassToAugment;
        private readonly IMethodSymbol? _getEnumeratorMethod;
        private readonly ITypeSymbol? _enumeratorType;
        private readonly IPropertySymbol? _enumeratorCurrentProperty;
        private readonly IMethodSymbol? _enumeratorMoveNextMethod;

        private sealed record UninitializedSymbols(INamedTypeSymbol NamedType, IPropertySymbol PropertySymbol,
            IMethodSymbol MethodSymbol);

        private static readonly LocklessWriteOnce<UninitializedSymbols> TheUninitializedNamedTypeSymbol;
        

    }

    public sealed class SemanticData : IEquatable<SemanticData>
    {
        public ref readonly EnableAugmentedEnumerationTargetTypeData TargetTypeData => ref _targetTypeData;
        public ref readonly AttribTargetData AttributeTargetData => ref _attribInfo;

        internal SemanticData(in EnableAugmentedEnumerationTargetTypeData ttd, in AttribTargetData atd)
        {
            _attribInfo = atd;
            _targetTypeData = ttd;
            _stringRep = new LocklessLazyWriteOnce<string>(GetStringRep);
        }

        public bool Equals(SemanticData? other) =>
            _attribInfo == other?._attribInfo && _targetTypeData == other._targetTypeData;

        public override int GetHashCode()
        {
            int hash = _attribInfo.GetHashCode();
            unchecked
            {
                hash = (hash * 397) ^ _targetTypeData.GetHashCode();
            }

            return hash;
        }

        public override bool Equals(object? other) => Equals(other as SemanticData);

        public static bool operator ==(SemanticData? lhs, SemanticData? rhs) =>
            ReferenceEquals(lhs, rhs) || lhs?.Equals(rhs) == true;

        public static bool operator !=(SemanticData? lhs, SemanticData? rhs) => !(lhs == rhs);

        public override string ToString() => _stringRep.Value;

        private string GetStringRep()
        {
            const string semDat = nameof(SemanticData);
            const string formatStr = "{0} -- Attribute Target Data: \"{1}\"; Target Type Data: \"{2}\".";
            string attribDat = _attribInfo.ToString();
            string targTypeData = _targetTypeData.ToString();
            return string.Format(formatStr, semDat, attribDat, targTypeData);
        }

        private readonly EnableAugmentedEnumerationTargetTypeData _targetTypeData;
        private readonly AttribTargetData _attribInfo;
        private readonly LocklessLazyWriteOnce<string> _stringRep;
    }
    public readonly struct AttribTargetData : IEquatable<AttribTargetData>, IHasGenericByRefRoEqComparer<AttribTargetData.EqComp, AttribTargetData>
    {
        public static AttribTargetData CreateTargetData(SemanticModel m, INamedTypeSymbol attribTs, SymbolInfo si) =>
            new(m, attribTs, si);

        public SemanticModel Model { get; }

        public INamedTypeSymbol AttributeTypeSymbol { get; }

        public SymbolInfo SymbolInformation { get; }

        private AttribTargetData(SemanticModel model, INamedTypeSymbol attribTypeSymbol, SymbolInfo si)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            AttributeTypeSymbol = attribTypeSymbol ?? throw new ArgumentNullException(nameof(attribTypeSymbol));
            SymbolInformation = si;
        }
        public override int GetHashCode()
        {
            int hash = Model.GetHashCode();
            unchecked
            {
                hash = (hash * 397) ^ SymbolEqualityComparer.Default.GetHashCode(AttributeTypeSymbol);
                hash = (hash * 397) ^ SymbolInformation.GetHashCode();
            }
            return hash;
        }

        public static bool operator ==(in AttribTargetData l, in AttribTargetData r) => l.Model == r.Model &&
            SymbolEqualityComparer.Default.Equals(l.AttributeTypeSymbol, r.AttributeTypeSymbol) &&
            l.SymbolInformation.Equals(r.SymbolInformation);
        public static bool operator !=(in AttribTargetData l, in AttribTargetData r) => !(l == r);

        /// <inheritdoc />
        public EqComp GetComparer() => default;
        

        public override bool Equals(object? other) => other is AttribTargetData std && std == this;
        public bool Equals(AttribTargetData std) => std == this;
        /// <inheritdoc />
        public override string ToString() =>
            $"{nameof(AttribTargetData)}-- Model assembly: {Model.Compilation.AssemblyName}, Attribute Symbol: {AttributeTypeSymbol.Name}, " +
            $"Symbol Info: {SymbolInformation.Symbol?.ToString() ?? "UNKNOWN/ERROR"}";

        public void Deconstruct(out SemanticModel model, out INamedTypeSymbol attribSyn, out SymbolInfo si)
        {
            model = Model;
            attribSyn = AttributeTypeSymbol;
            si = SymbolInformation;
        }

        public readonly struct EqComp : IByRoRefEqualityComparer<AttribTargetData>
        {
            /// <inheritdoc />
            public bool Equals(in AttribTargetData lhs, in AttribTargetData rhs) => lhs == rhs;


            /// <inheritdoc />
            public int GetHashCode(in AttribTargetData val) => val.GetHashCode();

        }

    }

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

    public readonly struct
        EnableAugmentedEnumerationTargetTypeData : IEquatable<EnableAugmentedEnumerationTargetTypeData>, 
            IHasGenericByRefRoEqComparer<EnableAugmentedEnumerationTargetTypeData.EqComp, EnableAugmentedEnumerationTargetTypeData>
    {
        public static EnableAugmentedEnumerationTargetTypeData CreateFailureDoesNotSpecifyANamedTypeTargetTypeDataNoTsAvailable(
            AttributeArgumentSyntax firstAttribArg, Location attributeLocation, TypeOfExpressionSyntax toes,
            in TypeInfo ti, TypeKind tk)
        {
            if (firstAttribArg == null) throw new ArgumentNullException(nameof(firstAttribArg));
            if (attributeLocation == null) throw new ArgumentNullException(nameof(attributeLocation));
            if (toes == null) throw new ArgumentNullException(nameof(toes));
            
            string reasonForInvalidity =
                $"A type symbol for the type specified by the typeof expression could not be identified..";
            Location l = toes.GetLocation();
            TextSpan ts = toes.Span;
            return new EnableAugmentedEnumerationTargetTypeData(firstAttribArg, l, in ts, toes, reasonForInvalidity,
                in ti, tk, null);
        }

        public static EnableAugmentedEnumerationTargetTypeData CreateFailureDoesNotSpecifyANamedTypeTargetTypeData(
            AttributeArgumentSyntax firstAttribArg, Location attributeLocation, TypeOfExpressionSyntax toes,
            in TypeInfo ti, TypeKind tk, ITypeSymbol badTs)
        {
            if (firstAttribArg == null) throw new ArgumentNullException(nameof(firstAttribArg));
            if (attributeLocation == null) throw new ArgumentNullException(nameof(attributeLocation));
            if (toes == null) throw new ArgumentNullException(nameof(toes));
            if (badTs == null) throw new ArgumentNullException(nameof(badTs));

            string reasonForInvalidity =
                $"The type specified by typeof expression is not a named type symbol.  It is of type \"{badTs.GetType().Name}\" and has value: {badTs}.";
            Location l = toes.GetLocation();
            TextSpan ts = toes.Span;
            return new EnableAugmentedEnumerationTargetTypeData(firstAttribArg, l, in ts, toes, reasonForInvalidity,
                in ti, tk, null);
        }
        public static EnableAugmentedEnumerationTargetTypeData CreateFailureAttributeLacksArgumentList(
            AttributeSyntax attribSyntax)
        {
            if (attribSyntax == null) throw new ArgumentNullException(nameof(attribSyntax));

            const string badnessReason = "The attribute syntax lacks an argument list.";
            Location l = attribSyntax.GetLocation();
            TextSpan attribSpan = attribSyntax.Span;
            return new EnableAugmentedEnumerationTargetTypeData(null, l, in attribSpan, null, badnessReason, default,
                TypeKind.Unknown, null);
        }

        public static EnableAugmentedEnumerationTargetTypeData CreateFailureFirstTypeArgIsNotTypeofExpressionSyntax(
            AttributeArgumentSyntax attribSyntax, ExpressionSyntax expr)
        {
            const string badnessReason = "The attribute's first argument is not a valid typeof expression.";
            Location l = expr.GetLocation();
            TextSpan s = expr.Span;
            return new EnableAugmentedEnumerationTargetTypeData(attribSyntax, l, in s, null, badnessReason, default,
                TypeKind.Unknown, null);
        }

        public static EnableAugmentedEnumerationTargetTypeData CreateSuccessTargetTypeData(
            AttributeArgumentSyntax firstAttribArg, Location attributeLocation, TypeOfExpressionSyntax toes,
            in TypeInfo ti, TypeKind tk, INamedTypeSymbol targetNts)
        {
            if (firstAttribArg == null) throw new ArgumentNullException(nameof(firstAttribArg));
            if (attributeLocation == null) throw new ArgumentNullException(nameof(attributeLocation));
            if (toes == null) throw new ArgumentNullException(nameof(toes));
            if (targetNts == null) throw new ArgumentNullException(nameof(targetNts));

            string reasonForInvalidity = string.Empty;
            Location l = toes.GetLocation();
            TextSpan ts = toes.Span;
            return new EnableAugmentedEnumerationTargetTypeData(firstAttribArg, l, in ts, toes, reasonForInvalidity,
                in ti, tk, targetNts);

        }

        public bool IsGoodMatch => FirstArgument != null && TypeOfSyntax != null && TargetTypeSymbol != null;

        public AttributeArgumentSyntax? FirstArgument => _firstArgument;
        public Location AttributeOrErrorLocation => _attributeOrErrorLocation ?? Location.None;
        public TypeOfExpressionSyntax? TypeOfSyntax => _typeOfSyntax;
        public TextSpan TargetTypeOrErrorTextSpan => _errorOrTargetTypeExpressionSpan;
        public string ReasonForInvalidity => _reasonForInvalidity ?? DefaultBadReason;
        public TypeInfo TargetTypeInformation => _typeInfo;
        public TypeKind TargetTypeKind => _typeKind;
        public INamedTypeSymbol? TargetTypeSymbol => _targetNts;

        /// <inheritdoc />
        public override int GetHashCode()
        {
            int hash = _typeKind.GetHashCode();
            unchecked
            {
                hash = (hash * 397) ^ (_targetNts == null
                    ? int.MinValue
                    : SymbolEqualityComparer.Default.GetHashCode(_targetNts));
                hash = (hash * 397) ^ AttributeOrErrorLocation.GetHashCode();
                hash = (hash * 397) ^ _errorOrTargetTypeExpressionSpan.GetHashCode();
            }
            return hash;
        }

        /// <inheritdoc />
        public EqComp GetComparer() => default;

        public override bool Equals(object? other) =>
            other is EnableAugmentedEnumerationTargetTypeData ttd && ttd == this;

        public bool Equals(EnableAugmentedEnumerationTargetTypeData other) => other == this;

        public static bool operator ==(in EnableAugmentedEnumerationTargetTypeData lhs,
            in EnableAugmentedEnumerationTargetTypeData rhs)
        {
            return AreEqual(lhs._firstArgument, rhs._firstArgument) && AreEqual(lhs._typeOfSyntax, rhs._typeOfSyntax) &&
                   AreEqual(lhs.AttributeOrErrorLocation, rhs.AttributeOrErrorLocation) &&
                   lhs._errorOrTargetTypeExpressionSpan == rhs._errorOrTargetTypeExpressionSpan &&
                   SymbolEqualityComparer.Default.Equals(lhs._targetNts, rhs._targetNts) &&
                   StringComparer.Ordinal.Equals(lhs.ReasonForInvalidity, rhs.ReasonForInvalidity) &&
                   lhs._typeInfo.Equals(rhs._typeInfo) && lhs._typeKind == rhs._typeKind;

            static bool AreEqual<T>(T? l, T? r) where T : class => ReferenceEquals(l, r) || l?.Equals(r) == true;
        }

        public static bool operator !=(in EnableAugmentedEnumerationTargetTypeData lhs,
            in EnableAugmentedEnumerationTargetTypeData rhs) => !(lhs == rhs);

        /// <inheritdoc />
        public override string ToString()
            //IsGoodMatch implies accessed nullable attributes are non-null
            => IsGoodMatch
                ? $"Typeof expression: {TypeOfSyntax!} yields a target {nameof(TypeKind)} of {TargetTypeKind} and a type symbol with name \"{_targetNts!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}\"."
                : $"The attribute does not have a valid target type.  Reason: \"{ReasonForInvalidity}\".";


        private EnableAugmentedEnumerationTargetTypeData(AttributeArgumentSyntax? aas, Location? place, in TextSpan ts,
            TypeOfExpressionSyntax? toes, string? reasonForInvalidity, in TypeInfo ti, TypeKind tk, INamedTypeSymbol? nts)
        {
            _firstArgument = aas;
            _attributeOrErrorLocation = place ?? Location.None;
            _errorOrTargetTypeExpressionSpan = ts;
            _typeOfSyntax = toes;
            _reasonForInvalidity = reasonForInvalidity ?? string.Empty;
            _typeInfo = ti;
            _typeKind = tk;
            _targetNts = nts;
        }

        private readonly AttributeArgumentSyntax? _firstArgument;
        private readonly Location? _attributeOrErrorLocation;
        private readonly TextSpan _errorOrTargetTypeExpressionSpan;
        private readonly TypeOfExpressionSyntax? _typeOfSyntax;
        private readonly string? _reasonForInvalidity;
        private readonly TypeInfo _typeInfo;
        private readonly TypeKind _typeKind;
        private readonly INamedTypeSymbol? _targetNts;

         
        private const string DefaultBadReason = "The " + nameof(EnableAugmentedEnumerationTargetTypeData) +
                                                " struct is not properly initialized.";

        public readonly struct EqComp : IByRoRefEqualityComparer<EnableAugmentedEnumerationTargetTypeData>
        {
            public bool Equals(in EnableAugmentedEnumerationTargetTypeData l,
                in EnableAugmentedEnumerationTargetTypeData r) => l == r;

            public int GetHashCode(in EnableAugmentedEnumerationTargetTypeData o) => o.GetHashCode();
        }


        public static EnableAugmentedEnumerationTargetTypeData CreateFailureBadTypeKind(AttributeArgumentSyntax firstParam, Location location, TypeOfExpressionSyntax toes, in TypeInfo ti, string reasonWhyNot, TypeKind tk, ITypeSymbol? badTs)
        {
            if (firstParam == null) throw new ArgumentNullException(nameof(firstParam));
            if (location == null) throw new ArgumentNullException(nameof(location));
            if (toes == null) throw new ArgumentNullException(nameof(toes));
            if (reasonWhyNot == null) throw new ArgumentNullException(nameof(reasonWhyNot));
            if (string.IsNullOrWhiteSpace(reasonWhyNot))
            {
                reasonWhyNot = "No reason available.";
            }

            if (badTs != null)
            {
                reasonWhyNot +=
                    $" Type symbol was of type {badTs.GetType().Name} and string rep is \"{badTs.ToDisplayString()}\".";
            }

            return new EnableAugmentedEnumerationTargetTypeData(firstParam, location, toes.Span, toes, reasonWhyNot,
                in ti, tk, null);
        }
    }
}
