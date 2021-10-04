using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cjm.Templates.Attributes;
using Cjm.Templates.Utilities;
using Cjm.Templates.Utilities.SetOnce;
using JetBrains.Annotations;
using LoggerLibrary;
using Microsoft.CodeAnalysis;
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
                        await ProcessInstantiations(context, tmplRcvr, context.CancellationToken);
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

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
        }

        private sealed record TemplateInterfaceSemanticData(INamedTypeSymbol InterfaceNamedTypeSymbol, ImmutableArray<TypeParameterWithConstraints> TypeArguments);
        



        private Task ProcessInstantiations(GeneratorExecutionContext context,
            TemplateInstantiatorSyntaxReceiver receiver, CancellationToken token)
        {
            try
            {

                ImmutableDictionary<FoundTemplateInterfaceRecord, TemplateInterfaceSemanticData> dict =
                    (from item in receiver.FoundInterfaceRecords
                        let tree = item.TemplateInterface.SyntaxTree
                        where tree != null
                        let model = context.Compilation.GetSemanticModel(tree, true)
                        where TrueOrThrowIfCancReq(token)
                        let interfaceSymbol = model.GetDeclaredSymbol(item.TemplateInterface) as INamedTypeSymbol
                        where interfaceSymbol != null
                        let typeParameters = ExtractInterfaceTArgsWithConstraints(interfaceSymbol.TypeParameters)
                        select new KeyValuePair<FoundTemplateInterfaceRecord, TemplateInterfaceSemanticData>(item,
                            new TemplateInterfaceSemanticData(interfaceSymbol, typeParameters)))
                    .ToImmutableDictionary();





               


                foreach (var (instantiationName, typeDeclarationSyntax, interfaceOrImplToInstantiate, isInstantiationTargetTemplateInterface) in receiver.FoundInstantiationRecords)
                {
                    INamedTypeSymbol? targetImpl;
                    INamedTypeSymbol? targetInterface;
                    var model = context.Compilation.GetSemanticModel(typeDeclarationSyntax.SyntaxTree, true);
                    TypeInfo ts = model.GetTypeInfo(interfaceOrImplToInstantiate.Type);
                    if (ts.ConvertedType is INamedTypeSymbol nts)
                    {
                        if (isInstantiationTargetTemplateInterface)
                        {
                            targetInterface = nts;
                            var candidates =
                                FindCandidatesForInstantiation(nts, context, receiver, token, model).ToImmutableArray();
                            if (candidates.Any())
                            {
                                DebugLog.LogMessage($"Found {candidates.Length} candidates.");
                            }

                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                TraceLog.LogMessage(
                    $"{nameof(TemplateInstantiator)}'s {nameof(ProcessInstantiations)} task was cancelled at [{StampSource.StampNow}] " +
                    $"with a receiver payload of {receiver.FoundInstantiationRecords.Length} instantiations.");
            }
            catch (Exception ex)
            {
                MonotonicStamp stamp = StampSource.StampNow;
                TraceLog.LogException(ex);
                TraceLog.LogError(
                    $"Unexpected exception of type \"{ex.GetType().Name}\" with message " +
                    $"\"{ex.Message}\" was thrown at [{stamp.ToString()}] during execution " +
                    $"of {nameof(TemplateInstantiator)}'s {nameof(ProcessInstantiations)} task.");
                throw;
            }

            return Task.CompletedTask;
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
        private IEnumerable<FoundTemplateImplementationRecord> FindCandidatesForInstantiation(INamedTypeSymbol nts, GeneratorExecutionContext context, TemplateInstantiatorSyntaxReceiver receiver, CancellationToken token, SemanticModel model)
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
                let ti = context.Compilation.GetTypeByMetadataName(firstArg.ToString())
                    where ti != null && SymbolEqualityComparer.Default.Equals(ti, nts)
                select item;
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
