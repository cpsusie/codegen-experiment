using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Cjm.Templates.Attributes;
using Cjm.Templates.Utilities;
using Cjm.Templates.Utilities.SetOnce;
using HpTimeStamps;
using LoggerLibrary;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cjm.Templates
{
    

    [Generator]
    public sealed class TemplateInstantiator : ISourceGenerator, IDisposable
    {
        /// <inheritdoc />
        public void Initialize(GeneratorInitializationContext context)
        {
            using var eel =
                LoggerSource.Logger.CreateEel(nameof(TemplateInstantiator), nameof(Initialize), context.ToString());
            context.RegisterForSyntaxNotifications(() => new TemplateInstantiatorSyntaxReceiver());
        }

        /// <inheritdoc />
        public void Execute(GeneratorExecutionContext context)
        {
            using var eel = TraceLog.CreateEel(nameof(TemplateInstantiator), nameof(Execute), context.ToString());
            if (context.SyntaxReceiver is TemplateInstantiatorSyntaxReceiver tmplRcvr)
            {
                tmplRcvr.Freeze();
                TraceLog.LogMessage(
                    $"Received a syntax receiver with {tmplRcvr.FoundInterfaceRecords.Length} template interface records.");
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

            }
        }

        private LocklessSetOnlyFlag _disposed;
    }

    public sealed class TemplateInstantiatorSyntaxReceiver : ISyntaxReceiver
    {
        public ImmutableArray<FoundTemplateInterfaceRecord> FoundInterfaceRecords => _freezeFlag.IsFrozen
            ? _templateInterfaceRecords
            : ImmutableArray<FoundTemplateInterfaceRecord>.Empty;
        

        public TemplateInstantiatorSyntaxReceiver()
        {
            _templateInterfaceRecordsBldr = ImmutableArray.CreateBuilder<FoundTemplateInterfaceRecord>();
        }

        /// <inheritdoc />
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            (ExtractionResult extractionResult, TypeDeclarationSyntax? declaredType, AttributeSyntax? matchingAttrib) =
                Extract(syntaxNode);
            Debug.Assert(!extractionResult.IsSuccessResult() || (declaredType != null && matchingAttrib != null));
            if (matchingAttrib != null && extractionResult.IsInterfaceSpecific() )
            {
                AddToIfClear(Volatile.Read(ref _templateInterfaceRecordsBldr), new FoundTemplateInterfaceRecord(declaredType ?? throw new ArgumentNullException(nameof(declaredType)), matchingAttrib));
            }
        }

        (ExtractionResult Result, TypeDeclarationSyntax? DeclaringType, AttributeSyntax? AttributesFound) Extract(
            SyntaxNode node)
        {
            ExtractionResult result = ExtractionResult.NotApplicable;
            TypeDeclarationSyntax? declaringType = null;
            AttributeSyntax? attribSyntax = null;
            switch (node)
            {
                case InterfaceDeclarationSyntax ids:
                    (result, declaringType, attribSyntax) = ExtractFromIds(ids);
                    break;
                case TypeDeclarationSyntax tds:
                    (result, declaringType, attribSyntax) = ExtractFrom(tds);
                    break;
            }

            return (result, declaringType, attribSyntax);

            static (ExtractionResult result, TypeDeclarationSyntax DeclaringType, AttributeSyntax? AttributesFound)
                ExtractFromIds(InterfaceDeclarationSyntax id)
            {
                bool errors;
                AttributeSyntax? matchingAttribute=null;
                ImmutableArray<AttributeSyntax> appliedAttributes =
                    id.AttributeLists.SelectMany(itm => itm.Attributes).ToImmutableArray();
                var matchingTemplateInterface =
                    (appliedAttributes.Where(itm => itm.Name.ToString() == CjmTemplateInterfaceAttribute.ShortName)).ToImmutableArray();
                var matchingImplementation = (appliedAttributes.Where(itm =>
                    itm.Name.ToString() == CjmTemplateImplementationAttribute.ShortName)).ToImmutableArray();
                var matchingInstantiation = (appliedAttributes.Where(itm =>
                    itm.Name.ToString() == CjmTemplateInstantiationAttribute.ShortName)).ToImmutableArray();
                errors = matchingImplementation.Length != 0 || matchingInstantiation.Length != 0 || matchingTemplateInterface.Length is > 1 or < 0;
                TypeDeclarationSyntax declaringType = id;
                
                if (matchingTemplateInterface.Any())
                {
                    declaringType = id;
                    matchingAttribute = matchingImplementation.First();
                }
                else if (matchingImplementation.Any())
                {
                    errors = true;
                    matchingAttribute = matchingImplementation.First();
                }
                if (matchingInstantiation.Any())
                {
                    errors = true;
                    matchingAttribute = matchingInstantiation.First();
                }

                return (errors, declaringType, matchingAttribute) switch
                {
                    (false, {}x, {}y) => (ExtractionResult.InterfaceOk, x, y),
                    (false, {}x, null) => (ExtractionResult.NotApplicable, x, null),
                    (true, {}x, null) => (ExtractionResult.ErrorUnknown, x, null),
                    (true, {}x, {}y) => (ExtractionResult.InterfaceWithErrors, x, y)
                };
            }

            static (ExtractionResult result, TypeDeclarationSyntax DeclaringType, AttributeSyntax? AttributesFound)
                ExtractFrom(TypeDeclarationSyntax cs)
            {
                ExtractionResult result = ExtractionResult.ErrorUnknown;
                bool errors=false;
                AttributeSyntax? matchingAttribute = null;
                ImmutableArray<AttributeSyntax> appliedAttributes =
                    cs.AttributeLists.SelectMany(itm => itm.Attributes).ToImmutableArray();
                var matchingTemplateInterface =
                    (appliedAttributes.Where(itm => itm.Name.ToString() == CjmTemplateInterfaceAttribute.ShortName)).ToImmutableArray();
                var matchingImplementation = (appliedAttributes.Where(itm =>
                    itm.Name.ToString() == CjmTemplateImplementationAttribute.ShortName)).ToImmutableArray();
                var matchingInstantiation = (appliedAttributes.Where(itm =>
                    itm.Name.ToString() == CjmTemplateInstantiationAttribute.ShortName)).ToImmutableArray();
                errors = (cs is not ClassDeclarationSyntax && cs is not StructDeclarationSyntax) ||
                         matchingImplementation.Length > 1 || matchingInstantiation.Length > 1 ||
                         matchingTemplateInterface.Any();
                TypeDeclarationSyntax declaringType = cs;
                if (matchingImplementation.Any())
                {
                    errors = errors || declaringType is not ClassDeclarationSyntax &&
                        declaringType is not StructDeclarationSyntax || matchingImplementation.Length > 1;
                    matchingAttribute = matchingImplementation.First();
                    result = !errors ? ExtractionResult.ImplementationOk : ExtractionResult.ImplementationWithErrors;
                }

                if (matchingInstantiation.Any())
                {
                    if (matchingAttribute == null)
                    {
                        errors = errors || declaringType is not ClassDeclarationSyntax &&
                            declaringType is not StructDeclarationSyntax || matchingInstantiation.Length > 1;
                        matchingAttribute = matchingImplementation.First();
                        result = !errors ? ExtractionResult.InstantiationOk : ExtractionResult.InstantiationWithErrors;
                    }
                    else
                    {
                        errors = true;
                        result = ExtractionResult.InstantiationWithErrors;
                    }
                }
                
                if (matchingTemplateInterface.Any())
                {
                    if (matchingAttribute == null)
                    {
                        errors = errors || declaringType is not ClassDeclarationSyntax &&
                            declaringType is not StructDeclarationSyntax || matchingTemplateInterface.Length > 1;
                        matchingAttribute = matchingTemplateInterface.First();
                        result = !errors ? ExtractionResult.InterfaceOk : ExtractionResult.InterfaceWithErrors;
                    }
                    else
                    {
                        errors = true;
                        result = ExtractionResult.InterfaceWithErrors;
                    }
                }

                return (matchingAttribute, errors, result) switch
                {
                    (null, false, _) => (ExtractionResult.NotApplicable, declaringType, matchingAttribute),
                    (null, true, { } val) => (val switch
                        {
                            ExtractionResult.NotApplicable => ExtractionResult.ErrorUnknown,
                            ExtractionResult.ImplementationOk => ExtractionResult.ImplementationWithErrors,
                            ExtractionResult.InterfaceOk => ExtractionResult.InterfaceWithErrors,
                            ExtractionResult.InstantiationOk => ExtractionResult.InstantiationWithErrors,
                            _ => ExtractionResult.ErrorUnknown
                        },
                        declaringType, null),
                    ({ } ma, false, { } v) => (v, declaringType, ma),
                    ({ } ma, true, { } v) => (v switch
                    {
                        ExtractionResult.NotApplicable => ExtractionResult.ErrorUnknown,
                        ExtractionResult.ImplementationOk => ExtractionResult.ImplementationWithErrors,
                        ExtractionResult.InterfaceOk => ExtractionResult.InterfaceWithErrors,
                        ExtractionResult.InstantiationOk => ExtractionResult.InstantiationWithErrors,
                        _ => ExtractionResult.ErrorUnknown
                    }, declaringType, ma)

                };

            }

            Debug.Assert((declaringType == null || attribSyntax == null) ==
                         (result != ExtractionResult.ImplementationOk && result != ExtractionResult.InterfaceOk));
            return (result, declaringType, attribSyntax);
        }

        private void ThrowIfNotClear([CallerMemberName] string callingMethodName = "")
        {
            var code = _freezeFlag.Code;
            if (code != FreezeFlagCode.Clear)
            {
                throw new FreezableObjectNotWritableException(nameof(TemplateInstantiator), callingMethodName, code,
                    null);
            }
        }

        private void AddToIfClear<TAddMe>(ImmutableArray<TAddMe>.Builder? bldr, TAddMe addMe, [CallerMemberName] string callerName ="")
        {
            if (bldr == null)
            {
                ThrowIfNotClear(callerName);
                throw new LocklessMultiStepException($"Builder found null unexpectedly in {nameof(AddToIfClear)}.");
            }
            
            ThrowIfNotClear(callerName);
            bldr.Add(addMe);
            ThrowIfNotClear(callerName);
        }

        public void Freeze()
        {
            if (_freezeFlag.IsFrozen) return;
            if (_freezeFlag.TryBeginFreeze())
            {
                var bldr = Interlocked.Exchange(ref _templateInterfaceRecordsBldr, null);
                if (bldr == null)
                {
                    _freezeFlag.CancelFreezeOrThrow();
                    throw new LocklessMultiStepException("During a freeze operation, builder unexpectedly found null.");
                }

                _templateInterfaceRecords = bldr.Capacity == bldr.Count ? bldr.MoveToImmutable() : bldr.ToImmutable();
                _freezeFlag.FinishFreezeOrThrow();
            }
            else if (!_freezeFlag.IsFrozen)
            {
                throw new LocklessMultiStepException("Flag is already frozen or is being frozen on another thread.");
            }
        }

        private ImmutableArray<FoundTemplateInterfaceRecord> _templateInterfaceRecords;
        private ImmutableArray<FoundTemplateInterfaceRecord>.Builder? _templateInterfaceRecordsBldr;
        
        private readonly LocklessFreezeFlag _freezeFlag = new();
        
    }

    public enum ExtractionResult : byte
    {
        ErrorUnknown = 0,
        NotApplicable,
        InterfaceWithErrors,
        ImplementationWithErrors,
        InstantiationWithErrors,
        InterfaceOk,
        ImplementationOk,
        InstantiationOk,
    }

    public static class ExtractionResultExtensions
    {
        public static readonly ImmutableArray<ExtractionResult> AllDefinedResults;

        public static bool IsDefined(this ExtractionResult result) => AllDefinedResults.Contains(result);

        public static ExtractionResult ValueOrThrowIfNDef(this ExtractionResult result, string? argName) =>
            result.IsDefined()
                ? result
                : throw new UndefinedEnumArgumentException<ExtractionResult>(result, argName ?? nameof(result));
        public static ExtractionResult ValueOrDefaultIfNDef(this ExtractionResult result) =>
            result.IsDefined() ? result : ExtractionResult.ErrorUnknown;

        public static bool IsSuccessResult(this ExtractionResult result) => result == ExtractionResult.InterfaceOk ||
                                                                            result == ExtractionResult
                                                                                .ImplementationOk ||
                                                                            result == ExtractionResult.InstantiationOk;

        public static bool IsNullResult(this ExtractionResult result) => result == ExtractionResult.NotApplicable;

        public static bool IsErrorResult(this ExtractionResult result) =>
            !result.IsSuccessResult() && !result.IsNullResult();

        public static bool IsInterfaceSpecific(this ExtractionResult result) =>
            result == ExtractionResult.InterfaceOk || result == ExtractionResult.InterfaceWithErrors;
        public static bool IsImplementationSpecific(this ExtractionResult result) =>
            result == ExtractionResult.ImplementationOk || result == ExtractionResult.ImplementationWithErrors;

        public static bool IsInstantiationSpecific(this ExtractionResult result) =>
            result == ExtractionResult.InstantiationWithErrors || result == ExtractionResult.InstantiationOk;



        static ExtractionResultExtensions() => AllDefinedResults =
            Enum.GetValues(typeof(ExtractionResult)).Cast<ExtractionResult>().ToImmutableArray();
    }

    public readonly record struct FoundTemplateInterfaceRecord(TypeDeclarationSyntax TemplateInterface,
        AttributeSyntax TemplateAttribute);

    public sealed class FreezableObjectNotWritableException : InvalidOperationException
    {
        public string FreezableObjectName { get; }

        public FreezeFlagCode CodeAtMomentOfAttemptedWrite { get; }

        public FreezableObjectNotWritableException(string freezableObjectName, string? badMemberName,
            FreezeFlagCode code, Exception? inner) : base(
            CreateMessage(freezableObjectName ?? throw new ArgumentNullException(nameof(freezableObjectName)),
                badMemberName, code, inner), inner)
        {
            FreezableObjectName = freezableObjectName;
            CodeAtMomentOfAttemptedWrite = code;
        }

        private static string CreateMessage(string freezableObjectName, string? badMemberName, FreezeFlagCode code, Exception? inner)
        {
            const string illegalCallMsgFrmt = "Illegal attempt to write to object {0}{1}";
            string memberNameText = !string.IsNullOrWhiteSpace(badMemberName) ? $"'s {badMemberName} member." : ".";
            return string.Format(illegalCallMsgFrmt, freezableObjectName, memberNameText) +
                   $" At moment of call freeze state was [{code}]." +
                   (inner != null ? " Consult inner exception for details." : string.Empty);
        }
    }
}
