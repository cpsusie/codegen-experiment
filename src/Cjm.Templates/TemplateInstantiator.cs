using System;
using System.Collections.Immutable;
using Cjm.Templates.Utilities;
using LoggerLibrary;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonotonicContext = HpTimeStamps.MonotonicStampContext;

namespace Cjm.Templates
{
    using MonotonicStamp = HpTimeStamps.MonotonicTimeStamp<MonotonicContext>;
    using StampSource = HpTimeStamps.MonotonicTimeStampUtil<MonotonicContext>;

    public readonly record struct FoundTemplateInterfaceRecord(TypeDeclarationSyntax TemplateInterface,
        AttributeSyntax TemplateAttribute);

    [Generator]
    public sealed class TemplateInstantiator : ISourceGenerator, IDisposable
    {
        public event EventHandler<TemplateInterfaceRecordsIdentifiedEventArgs>? TemplateInterfaceRecordsFound; 

        public TemplateInstantiator()
        {
            _pump = EventPumpFactorySource.FactoryInstance(GetNextThreadName());
        }

        /// <inheritdoc />
        public void Initialize(GeneratorInitializationContext context)
        {
            using var eel =
                TraceLog.CreateEel(nameof(TemplateInstantiator), nameof(Initialize), context.ToString());
            context.RegisterForSyntaxNotifications(() => new TemplateInstantiatorSyntaxReceiver());
        }

        /// <inheritdoc />
        public void Execute(GeneratorExecutionContext context)
        {
            using var eel = TraceLog.CreateEel(nameof(TemplateInstantiator), nameof(Execute), context.ToString());
            if (context.SyntaxReceiver is TemplateInstantiatorSyntaxReceiver tmplRcvr)
            {
                tmplRcvr.Freeze();
                var stamp = StampSource.StampNow;
                TraceLog.LogMessage(
                    $"Received a syntax receiver with {tmplRcvr.FoundInterfaceRecords.Length} template interface records.");
                OnTemplateInterfaceRecordsFound(tmplRcvr.FoundInterfaceRecords, stamp);
                int count = 0;
                foreach (var item in tmplRcvr.FoundInterfaceRecords)
                {
                    TraceLog.LogMessage($" \tItem #{++count} of {tmplRcvr.FoundInterfaceRecords.Length}:  \t{item.ToString()}");
                }
                TraceLog.LogMessage($"Done logging the {tmplRcvr.FoundInterfaceRecords.Length} results. ");
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed.TrySet() && disposing)
            {
                TemplateInterfaceRecordsFound = null;
                _pump.Dispose();
            }
            TemplateInterfaceRecordsFound = null;
        }

        private void OnTemplateInterfaceRecordsFound(ImmutableArray<FoundTemplateInterfaceRecord> records, MonotonicStamp? stamp)
        {
            if (!records.IsDefault)
            {
                _pump.RaiseEvent(() => TemplateInterfaceRecordsFound?.Invoke(this,
                    new TemplateInterfaceRecordsIdentifiedEventArgs(records, stamp ?? StampSource.StampNow)));
            }
        }

        private static string GetNextThreadName() =>
            string.Format(EventPumpThreadNameFrmtStr, ThreadNamePrefix, TheULongProvider.NextValue);

        private readonly IEventPump _pump;
        private LocklessSetOnlyFlag _disposed;
        private static readonly AtomicULongProvider TheULongProvider = new();
        private const string EventPumpThreadNameFrmtStr
            = "{0}_{1}";
        private const string ThreadNamePrefix = nameof(TemplateInstantiator) + "_Thrd";

        
    }
}
