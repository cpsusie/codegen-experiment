using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Cjm.CodeGen.Attributes;
using Cjm.CodeGen.Exceptions;
using HpTimeStamps;
using LoggerLibrary;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
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
                        ImmutableSortedDictionary<ClassDeclarationSyntax, ImmutableArray<SemanticData>> immutable = lookup.MakeImmutable();
                        OnFinalPayloadCreated(immutable);
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
                        new GeneratorTestEnableAugmentSyntaxReceiverPayloadEventArgs(targetData.Value);
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

        private event EventHandler<GeneratorTestingEnableAugmentedEnumerationFinalPayloadEventArgs>? FinalPayloadCreatedImpl;
        private event EventHandler<GeneratorTestEnableAugmentSyntaxReceiverPayloadEventArgs>?
            MatchingSyntaxDetectedImpl;
        private event EventHandler<GeneratorTestEnableAugmentSemanticPayloadEventArgs>? SemanticPayloadFoundImpl;
        private LocklessSetOnlyFlag _isDisposed;
        private readonly IEventPump _eventPump = EventPumpFactorySource.FactoryInstance("TransFEnumGen");

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

    [Foobar(typeof(List<DateTime>))]
    internal sealed class LocklessConcreteType
    {


        public Type ConcreteType
        {
            get
            {
                Type? ret = _concreteType;
                if (ret == null)
                {
                    ret = Volatile.Read(ref _concreteType);
                    if (ret == null)
                    {
                        Type theType = InitType();
                        Debug.Assert(theType != null);
                        Interlocked.CompareExchange(ref _concreteType, theType, null);
                        ret = Volatile.Read(ref _concreteType);
                    }
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

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class FoobarAttribute : Attribute
    {
        public Type TheType { get; }
        public FoobarAttribute(Type myType) => TheType = myType ?? throw new ArgumentNullException(nameof(myType));

    }
}
