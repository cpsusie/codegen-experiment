using System;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Cjm.CodeGen
{

    internal sealed record GatheredData(EnumeratorData? Ed, INamedTypeSymbol? TargetItemType,
        INamedTypeSymbol? TargetTypeCollection, INamedTypeSymbol? StaticClassToAugment,
        IMethodSymbol? GetEnumeratorMethod, ITypeSymbol? EnumeratorType, IPropertySymbol? EnumeratorCurrentProperty,
        IMethodSymbol? EnumeratorMoveNextMethod, IMethodSymbol? EnumeratorResetMethod,
        IMethodSymbol? EnumeratorDisposeMethod) 
    {
        /// <inheritdoc />
        public override int GetHashCode()
        {
            const int vitalItemNullHash = 401;   
            const int otherItemNullHashCode = 3;
            const int otherItemNotNullHashCode = 7;
                
            int hash = GetHashFromUsing(GatheredDataComparisonHelper.TargetItemTypeComparer, TargetItemType);
            unchecked
            {
                hash = (hash * 397) ^ GetHashFromUsing(GatheredDataComparisonHelper.TargetCollectionTypeComparer, TargetTypeCollection);
                hash = (hash * 397) ^ GetHashFromUsing(GatheredDataComparisonHelper.StaticClassToAugmentComparer, StaticClassToAugment);
                hash = (hash * 397) ^ GetHashFromUsing(GatheredDataComparisonHelper.EnumeratorTypeComparer, EnumeratorType);
                hash = (hash * 397) ^ GetHashFromUsing(GatheredDataComparisonHelper.GetEnumeratorMethodComparer, GetEnumeratorMethod);
                
                hash = (hash * 397) ^ GetHashFromUsing(GatheredDataComparisonHelper.EnumeratorCurrentPropertyComparer, EnumeratorCurrentProperty);
                       //GatheredDataComparisonHelper.EnumeratorCurrentPropertyComparer.GetHashCode(
                       //    EnumeratorCurrentProperty);
                hash = (hash * 397) ^ (Ed?.GetHashCode() ?? vitalItemNullHash);
                hash = (hash * 397) ^
                       (EnumeratorMoveNextMethod != null ? otherItemNotNullHashCode : otherItemNullHashCode);
                hash = (hash * 397) ^
                       (EnumeratorResetMethod != null ? otherItemNotNullHashCode : otherItemNullHashCode);
                hash = (hash * 397) ^ (EnumeratorDisposeMethod != null ? otherItemNotNullHashCode : otherItemNullHashCode);
            }
            return hash;

            static int GetHashFromUsing<TSymbol>(SymbolEqualityComparer eqComp, TSymbol? symbol) where TSymbol : ISymbol
            {
                const int vitalItemNullHashCode = 401;
                return symbol == null ? vitalItemNullHashCode : eqComp.GetHashCode(symbol);
            }
        }

        public bool Equals(GatheredData? other) => other != null && other.Ed == Ed && GatheredDataComparisonHelper.TargetItemTypeComparer.Equals(other.TargetItemType, TargetItemType) &&
                                                   GatheredDataComparisonHelper.TargetCollectionTypeComparer.Equals(other.TargetTypeCollection, TargetTypeCollection) &&
                                                   GatheredDataComparisonHelper.StaticClassToAugmentComparer.Equals(other.StaticClassToAugment, StaticClassToAugment) &&
                                                   GatheredDataComparisonHelper.EnumeratorCurrentPropertyComparer.Equals(other.EnumeratorCurrentProperty,
                                                       EnumeratorCurrentProperty) &&
                                                   GatheredDataComparisonHelper.GetEnumeratorMethodComparer.Equals(other.GetEnumeratorMethod, GetEnumeratorMethod) &&
                                                   GatheredDataComparisonHelper.MoveNextMethodComparer.Equals(other.EnumeratorMoveNextMethod, EnumeratorMoveNextMethod) &&
                                                   GatheredDataComparisonHelper.DisposeMethodComparer.Equals(other.EnumeratorDisposeMethod, EnumeratorDisposeMethod) &&
                                                   GatheredDataComparisonHelper.EnumeratorTypeComparer.Equals(other.EnumeratorType, EnumeratorType) &&
                                                   GatheredDataComparisonHelper.EnumeratorResetComparer.Equals(other.EnumeratorResetMethod, EnumeratorResetMethod);

    }

    public readonly struct GenerationData : IEquatable<GenerationData>, IHasGenericByRefRoEqComparer<GenerationData.EqComp, GenerationData>
    {
        public static readonly GenerationData InvalidDefault = default;

        internal static (GenerationData? GenerationData, GatheredDataSymbolAnalysisCode Code, EnumeratorDataCodeResult EdCodeResult, string AdditionalErrorInfo) TryCreateGenerationData(GatheredData gd)
        {
            if (gd == null) throw new ArgumentNullException(nameof(gd));
            
            (GatheredDataSymbolAnalysisCode symbolQueryResult, string extraSymbolErrorInfo) = QueryHasAllNeededSymbols(gd);
            GenerationData? dataRet = null;
            string extraInfo = string.Empty;
            EnumeratorDataCodeResult edDataRes = EnumeratorDataCodeResult.Ok;
            if (symbolQueryResult != GatheredDataSymbolAnalysisCode.Ok)
            {
                extraInfo = extraSymbolErrorInfo;
            }
            else if (gd.Ed is {} enumeratorData)
            {
                StringBuilder? log = null;
                if (enumeratorData.IsDataUnavailable)
                {
                    AppendFaultData(ref log, ref edDataRes, EnumeratorDataCodeResult.DataUnavailable,
                        "Enumerator data code has no data stored therein.");
                }

                if (enumeratorData.IsGenericIEnumerator)
                {
                    AppendFaultData(ref log, ref edDataRes, EnumeratorDataCodeResult.IsIEnumeratorT,
                        "The enumerator is of type IEnumeratorT itself, so use of this library for it is of no benefit.");
                }

                if (enumeratorData.IsIEnumerator)
                {
                    AppendFaultData(ref log, ref edDataRes, EnumeratorDataCodeResult.IsIEnumerator,
                        "The enumerator is of type IEnumerator itself, so use of this library for it is of no benefit.");
                }

                if (!enumeratorData.HasPublicMoveNext)
                {
                    AppendFaultData(ref log, ref edDataRes, EnumeratorDataCodeResult.LacksProperMoveNext,
                        "Enumerator type does not have a public MoveNext method.");
                }

                if (!enumeratorData.HasProperMoveNext)
                {
                    AppendFaultData(ref log, ref edDataRes, EnumeratorDataCodeResult.LacksProperMoveNext,
                        $"Enumerator type's public MoveNext method does not return {nameof(Boolean)} or does not have an empty parameter list. ");
                }

                if (!enumeratorData.EnumeratorHasPublicCurrent)
                {
                    AppendFaultData(ref log, ref edDataRes, EnumeratorDataCodeResult.LacksPublicCurrent,
                        "Enumerator type lacks a Public Current property with a public getter or it returns an invalid return type.");
                }

                if (edDataRes == EnumeratorDataCodeResult.Ok)
                {
                    dataRet = new GenerationData(enumeratorData, gd.TargetItemType!, gd.TargetTypeCollection,
                        gd.StaticClassToAugment!, gd.GetEnumeratorMethod!, gd.EnumeratorType!,
                        gd.EnumeratorCurrentProperty!, gd.EnumeratorMoveNextMethod!, gd.EnumeratorResetMethod,
                        gd.EnumeratorDisposeMethod);
                }
                else
                {
                    extraInfo = log?.ToString() ?? string.Empty;
                }
            }
            else
            {
                symbolQueryResult = GatheredDataSymbolAnalysisCode.MissingEnumeratorDataValue;
                extraInfo = "A value for enumerator data code was missing.";
            }

            Debug.Assert((dataRet == null) == (symbolQueryResult != GatheredDataSymbolAnalysisCode.Ok ||
                                               edDataRes != EnumeratorDataCodeResult.IsIEnumerator));
            return (dataRet, symbolQueryResult, edDataRes, extraInfo);

            static void AppendFaultData(ref StringBuilder? sb, ref EnumeratorDataCodeResult toUpdate,
                EnumeratorDataCodeResult flagToAdd, string errMsg)
            {
                sb ??= new();
                toUpdate |= flagToAdd;
                sb.AppendLine(errMsg);
            }
        }

        private static (GatheredDataSymbolAnalysisCode Result, string ExtraInfo) QueryHasAllNeededSymbols(GatheredData gd)
        {
            StringBuilder sb = new();
            GatheredDataSymbolAnalysisCode code = GatheredDataSymbolAnalysisCode.Ok;
            CheckForSymbolAndLogAbsence(gd.StaticClassToAugment, ref code,
                GatheredDataSymbolAnalysisCode.OtherMissingSymbolOrSymbols, sb, "Missing symbol for class to augment.");
            CheckForSymbolAndLogAbsence(gd.TargetTypeCollection, ref code,
                GatheredDataSymbolAnalysisCode.OtherMissingSymbolOrSymbols, sb, "No valid target class specified.");
            CheckForSymbolAndLogAbsence(gd.GetEnumeratorMethod, ref code, GatheredDataSymbolAnalysisCode.NoPublicGetEnumeratorMethod, sb);
            CheckForSymbolAndLogAbsence(gd.EnumeratorType, ref code , GatheredDataSymbolAnalysisCode.GetEnumeratorMethodDoesNotReturnValidObject, sb);
            CheckForSymbolAndLogAbsence(gd.EnumeratorCurrentProperty, ref code, GatheredDataSymbolAnalysisCode.EnumeratorReturnedLacksPublicCurrentGetterProperty, sb);
            CheckForSymbolAndLogAbsence(gd.EnumeratorMoveNextMethod, ref code, GatheredDataSymbolAnalysisCode.EnumeratorLacksPublicMoveNextPropertyReturningBool, sb);
            return (code, sb.ToString());

            static void CheckForSymbolAndLogAbsence<TSymbol>(TSymbol? symbol, ref GatheredDataSymbolAnalysisCode codeToUpdate, GatheredDataSymbolAnalysisCode failureCode, StringBuilder log, string? extraFailureMessage= null)
                where TSymbol : ISymbol
            {
                if (symbol == null)
                {
                    codeToUpdate |= failureCode;
                    if (!string.IsNullOrWhiteSpace(extraFailureMessage))
                        log.AppendLine(extraFailureMessage);
                }
            }
        }

        private GenerationData(EnumeratorData ed, INamedTypeSymbol targetItemType,
            INamedTypeSymbol? targetTypeCollection, INamedTypeSymbol staticClassToAugment,
            IMethodSymbol getEnumeratorMethod, ITypeSymbol enumeratorType, IPropertySymbol enumeratorCurrentProperty,
            IMethodSymbol enumeratorMoveNextMethod, IMethodSymbol? enumeratorResetMethod,
            IMethodSymbol? enumeratorDisposeMethod)
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
            int hash = GatheredDataComparisonHelper.TargetItemTypeComparer.GetHashCode(TargetItemType);
            unchecked
            {
                hash = (hash * 397) ^ GatheredDataComparisonHelper.TargetCollectionTypeComparer.GetHashCode(TargetCollectionType);
                hash = (hash * 397) ^ GatheredDataComparisonHelper.StaticClassToAugmentComparer.GetHashCode(StaticClassToAugment);
                hash = (hash * 397) ^ GatheredDataComparisonHelper.EnumeratorTypeComparer.GetHashCode(EnumeratorType);
                hash = (hash * 397) ^
                       GatheredDataComparisonHelper.GetEnumeratorMethodComparer.GetHashCode(GetEnumeratorMethod);
                hash = (hash * 397) ^
                       GatheredDataComparisonHelper.EnumeratorCurrentPropertyComparer.GetHashCode(
                           EnumeratorCurrentProperty);
                hash = (hash * 397) ^ _ed.GetHashCode();
            }
            return hash;
        }

        public static bool operator ==(in GenerationData lhs, in GenerationData rhs) => lhs._ed == rhs._ed &&
            GatheredDataComparisonHelper.TargetItemTypeComparer.Equals(lhs.TargetItemType, rhs.TargetItemType) &&
            GatheredDataComparisonHelper.TargetCollectionTypeComparer.Equals(lhs.TargetCollectionType, rhs.TargetCollectionType) &&
            GatheredDataComparisonHelper.StaticClassToAugmentComparer.Equals(lhs.StaticClassToAugment, rhs.StaticClassToAugment) &&
            GatheredDataComparisonHelper.EnumeratorCurrentPropertyComparer.Equals(lhs.EnumeratorCurrentProperty,
                rhs.EnumeratorCurrentProperty) &&
            GatheredDataComparisonHelper.GetEnumeratorMethodComparer.Equals(lhs.GetEnumeratorMethod, rhs.GetEnumeratorMethod) &&
            GatheredDataComparisonHelper.MoveNextMethodComparer.Equals(lhs.EnumeratorMoveNextMethod, rhs.EnumeratorMoveNextMethod) &&
            GatheredDataComparisonHelper.DisposeMethodComparer.Equals(lhs.EnumeratorDisposeMethod, rhs.EnumeratorDisposeMethod) &&
            GatheredDataComparisonHelper.EnumeratorTypeComparer.Equals(lhs.EnumeratorType, rhs.EnumeratorType) &&
            GatheredDataComparisonHelper.EnumeratorResetComparer.Equals(lhs.EnumeratorResetMethod, rhs.EnumeratorResetMethod);

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

    [Flags]
    public enum GatheredDataSymbolAnalysisCode : byte
    {
        Ok =                                                    0x00,
        NoPublicGetEnumeratorMethod =                           0x01,
        GetEnumeratorMethodDoesNotReturnValidObject=            0x02,
        EnumeratorReturnedLacksPublicCurrentGetterProperty=     0x04,
        EnumeratorLacksPublicMoveNextPropertyReturningBool=     0x08,
        OtherMissingSymbolOrSymbols =                           0x10,
        MissingEnumeratorDataValue =                            0x20,
        DataUnavailable =                                       0x40,
    }

    [Flags]
    public enum EnumeratorDataCodeResult : byte
    {
        Ok =                        0x00,
        DataUnavailable =           0x01,
        IsIEnumerator =             0x02,
        IsIEnumeratorT =            0x04,
        LacksPublicCurrent=         0x08,
        LacksMoveNext=              0x10,
        LacksProperMoveNext=        0x20,
    }

    internal static class GatheredDataComparisonHelper
    {
        public static SymbolEqualityComparer TargetItemTypeComparer => SymbolEqualityComparer.IncludeNullability;
        public static SymbolEqualityComparer TargetCollectionTypeComparer => SymbolEqualityComparer.Default;
        public static SymbolEqualityComparer StaticClassToAugmentComparer => SymbolEqualityComparer.Default;
        public static SymbolEqualityComparer EnumeratorCurrentPropertyComparer => SymbolEqualityComparer.IncludeNullability;
        public static SymbolEqualityComparer GetEnumeratorMethodComparer => SymbolEqualityComparer.IncludeNullability;
        public static SymbolEqualityComparer MoveNextMethodComparer => SymbolEqualityComparer.Default;
        public static SymbolEqualityComparer DisposeMethodComparer => SymbolEqualityComparer.Default;
        public static SymbolEqualityComparer EnumeratorTypeComparer => SymbolEqualityComparer.IncludeNullability;
        public static SymbolEqualityComparer EnumeratorResetComparer => SymbolEqualityComparer.Default;
    }
}