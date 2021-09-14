using System;
using Microsoft.CodeAnalysis;

namespace Cjm.CodeGen
{
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
}