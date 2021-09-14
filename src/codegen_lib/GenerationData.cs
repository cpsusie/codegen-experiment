using System;
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