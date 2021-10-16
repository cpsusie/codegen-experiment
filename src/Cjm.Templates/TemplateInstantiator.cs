using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cjm.Templates.Attributes;
using Cjm.Templates.Utilities;
using Cjm.Templates.Utilities.SetOnce;
using JetBrains.Annotations;
using LoggerLibrary;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonotonicContext = HpTimeStamps.MonotonicStampContext;

namespace Cjm.Templates
{
    using MonotonicStamp = HpTimeStamps.MonotonicTimeStamp<MonotonicContext>;
    using StampSource = HpTimeStamps.MonotonicTimeStampUtil<MonotonicContext>;

    public readonly record struct TypeParameterWithConstraints(int TypeArgumentIndex,
        string GenericIdentifier, ImmutableArray<AttributeData> TemplateConstraints, ImmutableHashSet<ITypeSymbol> NormalTypeConstraints);

    public readonly record struct FoundTemplateImplementationRecord(string ImplementationName,
        TypeDeclarationSyntax DeclaringImplementation, AttributeSyntax TemplImplementationAttribute, string TemplateName, SyntaxTree ImplTree);

    public readonly record struct FoundTemplateInterfaceRecord(string TemplateName, TypeDeclarationSyntax TemplateInterface,
        AttributeSyntax TemplateAttribute);

    public readonly record struct FoundTemplateInstantiationRecord(string InstantiationName,
        TypeDeclarationSyntax InstantiationDeclaration, TypeOfExpressionSyntax InterfaceOrImplToInstantiate, bool IsInstantiationTargetTemplateInterface );

    [Generator]
    public sealed class TemplateInstantiator : ISourceGenerator, IDisposable
    {
        public event EventHandler<TemplateRecordsIdentifiedEventArgs<FoundTemplateInterfaceRecord>>? TemplateInterfaceRecordsFound;
        public event EventHandler<TemplateRecordsIdentifiedEventArgs<FoundTemplateImplementationRecord>>? TemplateImplementationRecordsFound;
        public event EventHandler<TemplateRecordsIdentifiedEventArgs<FoundTemplateInstantiationRecord>>? TemplateInstantiationRecordsFound;
        public TemplateInstantiator() => _pump = EventPumpFactorySource.FactoryInstance(GetNextThreadName());

        /// <inheritdoc />
        public void Initialize(GeneratorInitializationContext context)
        {
            using var eel =
                TraceLog.CreateEel(nameof(TemplateInstantiator), nameof(Initialize), context.ToString());
            context.RegisterForSyntaxNotifications(() => new TemplateInstantiatorSyntaxReceiver());
            context.RegisterForPostInitialization(Callback);
        }

        private void Callback(GeneratorPostInitializationContext obj)
        {
            if (!_extraSources.IsSet)
            {
                int count = 0;
                var dict = ImmutableSortedDictionary.CreateBuilder<string, string>(TrimmedStringComparer
                    .TrimmedOrdinalIgnoreCase);
                foreach (string src in AdditionalTemplatesRepository.AdditionalTemplates)
                {
                    string name = count.ToString();
                    obj.AddSource(name, src);
                    dict.Add(name, src);
                    ++count;
                }

                bool setIt = _extraSources.TrySet(dict.ToImmutable());
                if (!setIt && !_extraSources.IsSet)
                    throw new InvalidOperationException("Unable to verify setting of extra sources.");
            }
        }

        /// <inheritdoc />
        public async void Execute(GeneratorExecutionContext context)
        {
            using var eel = TraceLog.CreateEel(nameof(TemplateInstantiator), nameof(Execute), context.ToString());
            try
            {
                CancellationToken token = context.CancellationToken;
                if (context.SyntaxReceiver is TemplateInstantiatorSyntaxReceiver tmplRcvr)
                {
                    tmplRcvr.Freeze();
                    ImmutableDictionary<FoundTemplateInstantiationRecord, ImmutableArray<Instantiator>> instantiatorLookup = ImmutableDictionary<FoundTemplateInstantiationRecord, ImmutableArray<Instantiator>>.Empty;
                    if (tmplRcvr.FoundInterfaceRecords.Any())
                    {
                        var stamp = StampSource.StampNow;
                        TraceLog.LogMessage(
                            $"Received a syntax receiver with {tmplRcvr.FoundInterfaceRecords.Length} template interface records.");
                        OnTemplateInterfaceRecordsFound(tmplRcvr.FoundInterfaceRecords, stamp);
                        int count = 0;
                        foreach (var item in tmplRcvr.FoundInterfaceRecords)
                        {
                            TraceLog.LogMessage(
                                $" \tItem #{++count} of {tmplRcvr.FoundInterfaceRecords.Length}:  \t{item.ToString()}");
                        }

                        TraceLog.LogMessage($"Done logging the {tmplRcvr.FoundInterfaceRecords.Length} results. ");
                    }
                    token.ThrowIfCancellationRequested();
                    if (tmplRcvr.FoundImplementationRecords.Any())
                    {
                        var stamp = StampSource.StampNow;
                        TraceLog.LogMessage(
                            $"Received a syntax receiver with {tmplRcvr.FoundImplementationRecords.Length} template implementation records.");
                        OnTemplateImplementationRecordsFound(tmplRcvr.FoundImplementationRecords, stamp);
                        int count = 0;
                        foreach (var item in tmplRcvr.FoundImplementationRecords)
                        {
                            TraceLog.LogMessage(
                                $" \tItem #{++count} of {tmplRcvr.FoundImplementationRecords.Length}:  \t{item.ToString()}");
                        }

                        TraceLog.LogMessage($"Done logging the {tmplRcvr.FoundImplementationRecords.Length} results. ");
                    }
                    token.ThrowIfCancellationRequested();
                    if (tmplRcvr.FoundInstantiationRecords.Any())
                    {
                        var stamp = StampSource.StampNow;
                        TraceLog.LogMessage(
                            $"Received a syntax receiver with {tmplRcvr.FoundInstantiationRecords.Length} template instantiation records.");
                        OnTemplateInstantiationRecordsFound(tmplRcvr.FoundInstantiationRecords, stamp);
                        int count = 0;
                        foreach (var item in tmplRcvr.FoundInstantiationRecords)
                        {
                            TraceLog.LogMessage(
                                $" \tItem #{++count} of {tmplRcvr.FoundInstantiationRecords.Length}:  \t{item.ToString()}");
                        }

                        TraceLog.LogMessage($"Done logging the {tmplRcvr.FoundInstantiationRecords.Length} results. ");
                        instantiatorLookup = await CreateInstantiators(context, tmplRcvr, context.CancellationToken);
                    }

                    if (instantiatorLookup.Any())
                    {
                        await ProcessInstantiationsAsync(instantiatorLookup, context, token);
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

        private Task ProcessInstantiationsAsync(
            ImmutableDictionary<FoundTemplateInstantiationRecord, ImmutableArray<Instantiator>> instantiatorLookup,
            GeneratorExecutionContext context, CancellationToken token) => Task.Run(async () =>
        {
            Task<string> instantiatorLogTask = LogInstantiators(instantiatorLookup, token);
            ImmutableArray<Task<ImmutableArray<(SyntaxNode Node, Instantiator Instantiator)>>> processTask = instantiatorLookup.Values
                .Select(val => ProcessInstantiation(val, context, token)).ToImmutableArray();
            string logMe = await instantiatorLogTask;
            TraceLog.LogMessage(logMe);
            foreach (var task in processTask)
            {
                token.ThrowIfCancellationRequested();
                await task;
            }

        }, token);

        private Task<ImmutableArray<(SyntaxNode Node, Instantiator Instantiator)>> ProcessInstantiation(ImmutableArray<Instantiator> instantiators, GeneratorExecutionContext context,
            CancellationToken token) => Task.Run(() =>
        {

            var bldr = ImmutableArray.CreateBuilder<(SyntaxNode Node, Instantiator Instantiator)>(instantiators.Length);
            foreach (var instantiator in instantiators)
            {
                var rewriter = new TemplateInstantiationSyntaxRewriter(instantiator, context, true);
                var root = instantiator.ImplData.ImplRecord.ImplTree.GetRoot();
                
                var x = rewriter.Visit(root);
                if (x != root)
                {
                    bldr.Add((x, instantiator));
                    TraceLog.LogMessage(x.GetText().ToString());
                }
            }
            return bldr.Count == bldr.Capacity ? bldr.MoveToImmutable() : bldr.ToImmutable();

        }, token);
        

        Task<string> LogInstantiators(
            ImmutableDictionary<FoundTemplateInstantiationRecord, ImmutableArray<Instantiator>> lookup,
            CancellationToken token) => Task.Run(() =>
        {
            token.ThrowIfCancellationRequested();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{lookup.Count} template instantiations were found: ");
            foreach (var kvp in lookup)
            {
                int candidateCount = kvp.Value.Length;
                sb.AppendLine($"\tThe instantiation [{kvp.Key}] has [{candidateCount}] candidates: ");
                int counter = 0;
                foreach (Instantiator item in kvp.Value)
                {
                    token.ThrowIfCancellationRequested();
                    sb.AppendLine($"\t\tCandidate# {++counter} of {candidateCount}: \t\t{item}");
                }
                sb.AppendLine($"\tDone printing candidates for [{kvp.Key}]");
            }
            sb.AppendLine($"Done with logging the {lookup.Count} instantiations.");
            return sb.ToString();
        }, token);
        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
        }

        private sealed record TemplateInterfaceSemanticData(INamedTypeSymbol InterfaceNamedTypeSymbol, ImmutableArray<TypeParameterWithConstraints> TypeArguments);
        



        private Task<ImmutableDictionary<FoundTemplateInstantiationRecord, ImmutableArray<Instantiator>>> CreateInstantiators(GeneratorExecutionContext context,
            TemplateInstantiatorSyntaxReceiver receiver, CancellationToken token)
        {
            try
            {
                var instantiators = ImmutableArray.CreateBuilder<KeyValuePair<FoundTemplateInstantiationRecord, Instantiator>>();
                //ImmutableDictionary<FoundTemplateInterfaceRecord, TemplateInterfaceSemanticData> dict =
                //    (from item in receiver.FoundInterfaceRecords
                //        let tree = item.TemplateInterface.SyntaxTree
                //        where tree != null
                //        let model = context.Compilation.GetSemanticModel(tree, true)
                //        where TrueOrThrowIfCancReq(token)
                //        let interfaceSymbol = model.GetDeclaredSymbol(item.TemplateInterface) as INamedTypeSymbol
                //        where interfaceSymbol != null
                //        let typeParameters = ExtractInterfaceTArgsWithConstraints(interfaceSymbol.TypeParameters)
                //        select new KeyValuePair<FoundTemplateInterfaceRecord, TemplateInterfaceSemanticData>(item,
                //            new TemplateInterfaceSemanticData(interfaceSymbol, typeParameters)))
                //    .ToImmutableDictionary();
                
                foreach (ref readonly FoundTemplateInstantiationRecord foundTemplateInstantiationRecord in receiver.FoundInstantiationRecords.WrapForByRefEnum())
                {
                    INamedTypeSymbol? targetInterface;
                    var model = context.Compilation.GetSemanticModel(foundTemplateInstantiationRecord.InstantiationDeclaration.SyntaxTree, true);
                    TypeInfo ts = ModelExtensions.GetTypeInfo(model, foundTemplateInstantiationRecord.InterfaceOrImplToInstantiate.Type);
                    if (ts.ConvertedType is INamedTypeSymbol nts)
                    {
                        if (foundTemplateInstantiationRecord.IsInstantiationTargetTemplateInterface)
                        {
                            targetInterface = nts;
                            token.ThrowIfCancellationRequested();
                            var candidates =
                                FindCandidatesForInstantiation(nts, context, receiver, token).ToImmutableArray();
                            token.ThrowIfCancellationRequested();
                            if (candidates.Any())
                            {
                                DebugLog.LogMessage($"Found {candidates.Length} candidates.");
                                foreach (var candidate in candidates)
                                {
                                    ImmutableArray<ITypeParameterSymbol> implementationParameterSymbols =
                                        candidate.TemplateInterfaceTypeSymbol.TypeParameters.ToImmutableArray();
                                    ImmutableArray<ITypeParameterSymbol> interfaceParameterSymbols =
                                        targetInterface.TypeParameters.ToImmutableArray();
                                    ImmutableArray<ITypeSymbol> symbolsToSub = ModelExtensions.GetSymbolInfo(model, foundTemplateInstantiationRecord.InterfaceOrImplToInstantiate.Type, token).Symbol is
                                        INamedTypeSymbol toSubstitute
                                        ? toSubstitute.TypeArguments.ToImmutableArray()
                                        : ImmutableArray<ITypeSymbol>.Empty;
                                    ImmutableArray<TypeParameterSyntax> implementationTypeParams =
                                        candidate.ImplRecord.DeclaringImplementation.TypeParameterList?.Parameters
                                            .ToImmutableArray() ?? ImmutableArray<TypeParameterSyntax>.Empty;
                                    ImmutableArray<ITypeParameterSymbol?> symbols =
                                        implementationTypeParams.Select(itm =>
                                        model.GetSymbolInfo(itm).Symbol switch
                                            {
                                                null => null,
                                                ITypeParameterSymbol tps => tps,
                                                _ => null
                                            }).ToImmutableArray();
                                    token.ThrowIfCancellationRequested();
                                    if (symbolsToSub.Length == implementationParameterSymbols.Length &&
                                        implementationParameterSymbols.Length == interfaceParameterSymbols.Length &&
                                        implementationTypeParams.Length == symbolsToSub.Length)
                                    {
                                        TraceLog.LogMessage("Symbol lengths match.");
                                        instantiators.Add(new(foundTemplateInstantiationRecord,
                                            Instantiator.CreateInstantiator(implementationTypeParams, symbolsToSub,
                                                in foundTemplateInstantiationRecord, in candidate, targetInterface)));
                                    }

                                    //ImmutableArray<ITypeSymbol> instantiationSymbols = from item in interfaceOrImplToInstantiate
                                    //    let sodel = context.Compilation.GetSemanticModel(item, true)
                                        
                                    DebugLog.LogMessage($"# of {nameof(implementationParameterSymbols)}: {implementationParameterSymbols.Length}");
                                    DebugLog.LogMessage($"# of {nameof(interfaceParameterSymbols)}: {interfaceParameterSymbols.Length}");
                                }
                            }

                        }
                        
                    }
                }
                token.ThrowIfCancellationRequested();
                var dictBldr = ImmutableDictionary
                    .CreateBuilder<FoundTemplateInstantiationRecord, ImmutableArray<Instantiator>.Builder>();
                foreach (var kvp in instantiators)
                {
                    if (!dictBldr.ContainsKey(kvp.Key))
                    {
                        dictBldr.Add(kvp.Key, ImmutableArray.CreateBuilder<Instantiator>());
                    }
                    dictBldr[kvp.Key].Add(kvp.Value);
                }
                token.ThrowIfCancellationRequested();
                ImmutableDictionary<FoundTemplateInstantiationRecord, ImmutableArray<Instantiator>> ret = (from kvp in dictBldr
                        let key = kvp.Key
                        let value = kvp.Value
                        select new KeyValuePair<FoundTemplateInstantiationRecord, ImmutableArray<Instantiator>>(key,
                            value.Count == value.Capacity ? value.MoveToImmutable() : value.ToImmutable()))
                    .ToImmutableDictionary();
                return Task.FromResult(ret);
            }
            catch (OperationCanceledException)
            {
                TraceLog.LogMessage(
                    $"{nameof(TemplateInstantiator)}'s {nameof(CreateInstantiators)} task was cancelled at [{StampSource.StampNow}] " +
                    $"with a receiver payload of {receiver.FoundInstantiationRecords.Length} instantiations.");
                throw;
            }
            catch (Exception ex)
            {
                MonotonicStamp stamp = StampSource.StampNow;
                TraceLog.LogException(ex);
                TraceLog.LogError(
                    $"Unexpected exception of type \"{ex.GetType().Name}\" with message " +
                    $"\"{ex.Message}\" was thrown at [{stamp.ToString()}] during execution " +
                    $"of {nameof(TemplateInstantiator)}'s {nameof(CreateInstantiators)} task.");
                throw;
            }

         
        }

        private ImmutableArray<TypeParameterWithConstraints> ExtractInterfaceTArgsWithConstraints(
            ImmutableArray<ITypeParameterSymbol> items)
        {
            if (items.IsDefaultOrEmpty) return ImmutableArray<TypeParameterWithConstraints>.Empty;
            var bldr = ImmutableArray.CreateBuilder<TypeParameterWithConstraints>(items.Length);

            int idx = 0;
            foreach (ITypeParameterSymbol item in items)
            {
                bldr.Add(new TypeParameterWithConstraints(idx++, item.Name, item.GetAttributes(),
                    item.ConstraintTypes.Where(ct => ct != null)
                        .ToImmutableHashSet<ITypeSymbol>(SymbolEqualityComparer.Default)));
            }
            return bldr.MoveToImmutable();

        }
        private IEnumerable<FoundTemplateImplementationRecordWithTypeSymbolData> FindCandidatesForInstantiation(INamedTypeSymbol nts, GeneratorExecutionContext context, TemplateInstantiatorSyntaxReceiver receiver, CancellationToken token)
        {
            
            token.ThrowIfCancellationRequested();
            return  from item in receiver.FoundImplementationRecords
                where item.TemplImplementationAttribute != null && TrueOrThrowIfCancReq(token)
                let relevantAttribute = item.TemplImplementationAttribute
                let attribName = relevantAttribute.Name.ToString()
                where TrimmedStringComparer.TrimmedOrdinal.Equals(attribName, CjmTemplateImplementationAttribute.ShortName)
                let firstArg =
                    ((relevantAttribute.ArgumentList?.Arguments as IReadOnlyList<AttributeArgumentSyntax> ??
                      Array.Empty<AttributeArgumentSyntax>()).FirstOrDefault()?.Expression as TypeOfExpressionSyntax)
                    ?.Type
                where firstArg != null
                let model = context.Compilation.GetSemanticModel(firstArg.SyntaxTree, true)
                    where model != null && TrueOrThrowIfCancReq(token)
                                let ti = ModelExtensions.GetTypeInfo(model, firstArg, token)
                    where ti.ConvertedType != null && !nts.IsUnboundGenericType 
                        let x = (ti.ConvertedType, nts) switch
                        {
                            ({}ct, {}symbol ) when SymbolEqualityComparer.Default
                                .Equals(ti.ConvertedType, symbol) => (ValueTuple<INamedTypeSymbol, INamedTypeSymbol, INamedTypeSymbol>?) (ct, symbol, symbol),
                            ({} ct, {}symbol) when SymbolEqualityComparer.Default
                                .Equals(ti.ConvertedType, symbol.ConstructUnboundGenericType()) => (ValueTuple<INamedTypeSymbol, INamedTypeSymbol, 
                                INamedTypeSymbol>?)(ct, symbol, symbol.ConstructUnboundGenericType()),
                            _=> null
                        }
                    where x != null
                select new FoundTemplateImplementationRecordWithTypeSymbolData(item, x.Value.Item1, x.Value.Item2, x.Value.Item3);
        }

        private static bool TrueOrThrowIfCancReq(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return true;
        }

        private void Dispose(bool disposing)
        {
            if (_disposed.TrySet() && disposing)
            {
                TemplateInterfaceRecordsFound = null;
                TemplateImplementationRecordsFound = null;
                _pump.Dispose();
            }
            TemplateInterfaceRecordsFound = null;
            TemplateImplementationRecordsFound = null;
        }

        private void OnTemplateInterfaceRecordsFound(ImmutableArray<FoundTemplateInterfaceRecord> records, MonotonicStamp? stamp)
        {
            if (!records.IsDefault)
            {
                _pump.RaiseEvent(() => TemplateInterfaceRecordsFound?.Invoke(this,
                    new TemplateRecordsIdentifiedEventArgs<FoundTemplateInterfaceRecord>(stamp ?? StampSource.StampNow, records)));
            }
        }

        private void OnTemplateImplementationRecordsFound(ImmutableArray<FoundTemplateImplementationRecord> records, MonotonicStamp? stamp)
        {
            if (!records.IsDefault)
            {
                _pump.RaiseEvent(() => TemplateImplementationRecordsFound?.Invoke(this,
                    new TemplateRecordsIdentifiedEventArgs<FoundTemplateImplementationRecord>(
                        stamp ?? StampSource.StampNow, records)));
            }
        }

        private void OnTemplateInstantiationRecordsFound(ImmutableArray<FoundTemplateInstantiationRecord> records, MonotonicStamp? stamp)
        {
            if (!records.IsDefault)
            {
                _pump.RaiseEvent(() => TemplateInstantiationRecordsFound?.Invoke(this,
                    new TemplateRecordsIdentifiedEventArgs<FoundTemplateInstantiationRecord>(
                        stamp ?? StampSource.StampNow, records)));
            }
        }

        private static string GetNextThreadName() =>
            string.Format(EventPumpThreadNameFrmtStr, ThreadNamePrefix, TheULongProvider.NextValue);
        private readonly IEventPump _pump;
        private LocklessSetOnlyFlag _disposed;
        private readonly LocklessWriteOnce<ImmutableSortedDictionary<string, string>> _extraSources = new();
        private static readonly AtomicULongProvider TheULongProvider = new();
        private const string EventPumpThreadNameFrmtStr
            = "{0}_{1}";
        private const string ThreadNamePrefix = nameof(TemplateInstantiator) + "_Thrd";


      
    }

    sealed class LocklessWriteOnce<T> where T : class
    {
        public bool IsSet
        {
            get
            {
                T? test = Volatile.Read(ref _value);
                return test != null;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public T Value
        {
            get
            {
                T? test = _value;
                return test ?? throw new InvalidOperationException("The value has not yet been set.");
            }
        }

        public bool TrySet(T val)
        {
            if (val == null) throw new ArgumentNullException(nameof(val));
            return Interlocked.CompareExchange(ref _value, val, null) == null;
        }

        public void SetOrThrow(T val)
        {
            if (val == null) throw new ArgumentNullException(nameof(val));
            if (!TrySet(val))
            {
                Debug.Assert(_value != null);
                throw new LocklessFlagAlreadySetException<T>(val, _value!);
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"[{nameof(LocklessWriteOnce<T>)}] -- " +
                                             (IsSet ? $"\tValue: \t[{Value}]." : "[NOT SET].");
        

        private T? _value;
    }

    internal struct LocklessNonZeroInteger
    {
        public readonly bool IsSet
        {
            get
            {
                int val = _value;
                return val != 0;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int Value => IsSet ? _value : throw new InvalidOperationException("The value has not been set yet.");

        public bool TrySet(int wantToBe)
        {
            if (wantToBe == 0) throw new ArgumentException(@"Value cannot be zero.", nameof(wantToBe));
            const int needToBeNow = 0;
            return Interlocked.CompareExchange(ref _value, wantToBe, needToBeNow) == needToBeNow;
        }

        public void SetOrThrow(int wantToBe)
        {
            if (wantToBe == 0) throw new ArgumentException(@"Value cannot be zero.", nameof(wantToBe));
            if (!TrySet(wantToBe))
            {
                throw new LocklessFlagAlreadySetException<int>(wantToBe, _value);
            }
        }

        public override readonly string ToString() => $"[{nameof(LocklessNonZeroInteger)}] -- " +
                                                      (IsSet ? $"\tValue: \t[{_value}]." : "[NOT SET].");
        private volatile int _value;
    }
}
