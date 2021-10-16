using Microsoft.CodeAnalysis;

namespace Cjm.Templates
{
    public sealed record FoundTemplateImplementationRecordWithTypeSymbolData
    {
        

        public FoundTemplateImplementationRecordWithTypeSymbolData(in FoundTemplateImplementationRecord foundMe, INamedTypeSymbol templateInterfaceTypeSymbol,
            INamedTypeSymbol templateImplementationAttributeReferencingTemplateInterface,
            INamedTypeSymbol unboundGenericTemplImplAttribRefTemplInterface)
        {
            _implRecord = foundMe;
            TemplateInterfaceTypeSymbol = templateInterfaceTypeSymbol;
            TemplateImplementationAttributeReferencingTemplateInterface = templateImplementationAttributeReferencingTemplateInterface;
            UnboundGenericTemplImplAttribRefTemplInterface = unboundGenericTemplImplAttribRefTemplInterface;
        }

        public ref readonly FoundTemplateImplementationRecord ImplRecord => ref _implRecord;
        public INamedTypeSymbol TemplateInterfaceTypeSymbol { get; init; }
        public INamedTypeSymbol TemplateImplementationAttributeReferencingTemplateInterface { get; init; }
        public INamedTypeSymbol UnboundGenericTemplImplAttribRefTemplInterface { get; init; }

        public void Deconstruct(out FoundTemplateImplementationRecord implRecord, out INamedTypeSymbol templateInterfaceTypeSymbol, out INamedTypeSymbol templateImplementationAttributeReferencingTemplateInterface, out INamedTypeSymbol unboundGenericTemplImplAttribRefTemplInterface)
        {
            implRecord = ImplRecord;
            templateInterfaceTypeSymbol = TemplateInterfaceTypeSymbol;
            templateImplementationAttributeReferencingTemplateInterface = TemplateImplementationAttributeReferencingTemplateInterface;
            unboundGenericTemplImplAttribRefTemplInterface = UnboundGenericTemplImplAttribRefTemplInterface;
        }

        public bool Equals(FoundTemplateImplementationRecordWithTypeSymbolData? other) => other != null &&
            other._implRecord == _implRecord &&
            SymbolEqualityComparer.Default.Equals(TemplateInterfaceTypeSymbol, other.TemplateInterfaceTypeSymbol) &&
            SymbolEqualityComparer.Default.Equals(TemplateImplementationAttributeReferencingTemplateInterface,
                other.TemplateImplementationAttributeReferencingTemplateInterface) &&
            SymbolEqualityComparer.Default.Equals(UnboundGenericTemplImplAttribRefTemplInterface,
                other.UnboundGenericTemplImplAttribRefTemplInterface);

        public override int GetHashCode()
        {
            int hash = _implRecord.GetHashCode();
            unchecked
            {
                hash = (hash * 397) ^ SymbolEqualityComparer.Default.GetHashCode(TemplateInterfaceTypeSymbol);
                hash = (hash * 397) ^ SymbolEqualityComparer.Default.GetHashCode(TemplateImplementationAttributeReferencingTemplateInterface);
                hash = (hash * 397) ^ SymbolEqualityComparer.Default.GetHashCode(UnboundGenericTemplImplAttribRefTemplInterface);
            }
            return hash;
        }

        private readonly FoundTemplateImplementationRecord _implRecord;
    }
}