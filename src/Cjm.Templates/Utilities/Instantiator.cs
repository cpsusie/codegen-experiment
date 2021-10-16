using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Cjm.Templates.Utilities.SetOnce;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cjm.Templates.Utilities
{
    internal sealed class Instantiator
    {
        internal static Instantiator CreateInstantiator(ImmutableArray<TypeParameterSyntax> symbolsToBeReplaced,
            ImmutableArray<ITypeSymbol> replacementSymbols, in FoundTemplateInstantiationRecord fir, in FoundTemplateImplementationRecordWithTypeSymbolData impl, INamedTypeSymbol templateInterface)
        {
            if (templateInterface == null) throw new ArgumentNullException(nameof(templateInterface));
            if (symbolsToBeReplaced.IsDefault)
                throw new UninitializedStructArgumentException<ImmutableArray<TypeParameterSyntax>>(
                    nameof(symbolsToBeReplaced));
            if (replacementSymbols.IsDefault)
                throw new UninitializedStructArgumentException<ImmutableArray<ITypeSymbol>>();
            if (symbolsToBeReplaced.Any(itm => itm == null))
                throw new ArgumentException(@"Parameter contains one or more null references.",
                    nameof(symbolsToBeReplaced));
            if (replacementSymbols.Any(itm => itm == null))
                throw new ArgumentException(@"Parameter contains one or more null references.",
                    nameof(replacementSymbols));
            if (replacementSymbols.Length != symbolsToBeReplaced.Length)
                throw new ArgumentException(
                    $"Parameter {nameof(replacementSymbols)} has {replacementSymbols.Length} items; " +
                    $"Parameter {nameof(symbolsToBeReplaced)} has {symbolsToBeReplaced.Length} " +
                    $"items.  Do not call this method ({nameof(CreateInstantiator)}) if different counts.");
            if (replacementSymbols.Length == 0)
                throw new ArgumentException(
                    $"Parameters {nameof(symbolsToBeReplaced)} and {nameof(replacementSymbols)} contain zero items.  " +
                    $"Parameters must contain a positive number of items and the number of items must be the same in " +
                    $"each parameter.");

            var bldr = ImmutableArray.CreateBuilder<SubPair>(replacementSymbols.Length);
            for (int i = 0; i < replacementSymbols.Length; ++i)
            {
                bldr.Add(new SubPair(symbolsToBeReplaced[i], replacementSymbols[i]));
            }

            ImmutableArrayByRefAdapter<SubPair> immut =
                ImmutableArrayByRefAdapter<SubPair>.CreateDestructivelyFromBuilder(ref bldr);
            return new Instantiator(in immut, in fir, in impl, templateInterface);
        }

        public INamedTypeSymbol TargetInterface => _targetInterface;
        public ImmutableArrayByRefAdapter<SubPair> SubstitutionPairs => _subPairs;
        public ref readonly FoundTemplateInstantiationRecord InstantiationRecord => ref _fir;
        public FoundTemplateImplementationRecordWithTypeSymbolData ImplData => _implementationData;
        

        private Instantiator(in ImmutableArrayByRefAdapter<SubPair> pairs, in FoundTemplateInstantiationRecord record, in FoundTemplateImplementationRecordWithTypeSymbolData impl, INamedTypeSymbol targetInterface)
        {
            _subPairs = pairs;
            _fir = record;
            _implementationData = impl;
            _targetInterface = targetInterface ?? throw new ArgumentNullException(nameof(targetInterface));
            _stringRep = new LocklessLazyWriteOnce<string>(InitStringRep);
        }

        /// <inheritdoc />
        public override string ToString() => _stringRep.Value;
        

        private string InitStringRep()
        {
            int numSubs = _subPairs.Length;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(
                $"[{nameof(Instantiator)}] -- Will instantiate template interface {_targetInterface.Name} into {_fir.InstantiationName} via implementation provided in {_implementationData.ImplRecord.ImplementationName}.");
            sb.AppendLine($"There are {numSubs} symbols to substitute: ");
            int counter = 0;
            foreach (ref readonly SubPair pair in _subPairs)
            {
                sb.AppendLine(
                    $"\t\tSubstitution #{++counter} of {numSubs}: \t{pair.ToBeReplaced.Identifier.Text} -> {pair.ReplaceWithMe.Name}");
            }

            sb.AppendLine("Done listing subs.");
            return sb.ToString();
        }

        private readonly INamedTypeSymbol _targetInterface;
        private readonly LocklessLazyWriteOnce<string> _stringRep;
        private readonly ImmutableArrayByRefAdapter<SubPair> _subPairs;
        private readonly FoundTemplateInstantiationRecord _fir;
        private readonly FoundTemplateImplementationRecordWithTypeSymbolData _implementationData;
    }
}
