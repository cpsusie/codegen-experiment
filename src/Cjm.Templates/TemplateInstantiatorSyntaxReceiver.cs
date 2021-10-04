using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Cjm.Templates.Attributes;
using Cjm.Templates.Utilities;
using Cjm.Templates.Utilities.SetOnce;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cjm.Templates
{
    public sealed class TemplateInstantiatorSyntaxReceiver : ISyntaxReceiver
    {
        public ImmutableArray<FoundTemplateInterfaceRecord> FoundInterfaceRecords => _freezeFlag.IsFrozen
            ? _templateInterfaceRecords
            : ImmutableArray<FoundTemplateInterfaceRecord>.Empty;

        public ImmutableArray<FoundTemplateImplementationRecord> FoundImplementationRecords => _freezeFlag.IsFrozen
            ? _templateImplementationRecords
            : ImmutableArray<FoundTemplateImplementationRecord>.Empty;

        public ImmutableArray<FoundTemplateInstantiationRecord> FoundInstantiationRecords => _freezeFlag.IsFrozen
            ? _templateInstantRecords
            : ImmutableArray<FoundTemplateInstantiationRecord>.Empty;

        public TemplateInstantiatorSyntaxReceiver()
        {
            _templateInterfaceRecordsBldr = ImmutableArray.CreateBuilder<FoundTemplateInterfaceRecord>();
            _tempImplRecordsBldr = ImmutableArray.CreateBuilder<FoundTemplateImplementationRecord>();
            _tempInstantRecordsBldr = ImmutableArray.CreateBuilder<FoundTemplateInstantiationRecord>();
        }

        /// <inheritdoc />
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            (ExtractionResult extractionResult, TypeDeclarationSyntax? declaredType, AttributeSyntax? matchingAttrib) =
                Extract(syntaxNode);
            Debug.Assert(!extractionResult.IsSuccessResult() || (declaredType != null && matchingAttrib != null));
            if (matchingAttrib != null && declaredType != null )
            {
                string name = declaredType.Identifier.ToString();
                if (extractionResult.IsInterfaceSpecific())
                {
                    
                    AddToIfClear(Volatile.Read(ref _templateInterfaceRecordsBldr),
                        new FoundTemplateInterfaceRecord(name,
                            declaredType ?? throw new ArgumentNullException(nameof(declaredType)), matchingAttrib));
                }
                else if (extractionResult.IsImplementationSpecific())
                {
                    //todo fixit -- foobar
                    (bool ok, string templateIntfName) = ExtractTemplateNameFromAttributeSyntax(matchingAttrib);
                    AddToIfClear(Volatile.Read(ref _tempImplRecordsBldr),
                        new FoundTemplateImplementationRecord(name, declaredType, matchingAttrib, ok ? templateIntfName : $"ERROR: {templateIntfName}",
                            declaredType.SyntaxTree));
                }
                else if (extractionResult.IsInstantiationSpecific())
                {
                    (bool ok, TypeOfExpressionSyntax? toes, bool? targetIsTemplateInterface) =
                        ExtractInstantiationInfo(matchingAttrib);
                    Debug.Assert(!ok || (toes != null && targetIsTemplateInterface != null), "If ok, others not null.");
                    if (ok)
                    {
                        AddToIfClear(Volatile.Read(ref _tempInstantRecordsBldr), new FoundTemplateInstantiationRecord(name, declaredType, toes!, targetIsTemplateInterface!.Value));
                    }
                }

                static (bool Ok, string Name) ExtractTemplateNameFromAttributeSyntax(AttributeSyntax syntax)
                {
                    var x = syntax.ArgumentList?.Arguments.FirstOrDefault();
                    return x switch
                    {
                        null => (false, "ERROR CANNOT IDENTIFY TEMPLATE"),
                        { } y
                            when y.DescendantNodes().OfType<TypeOfExpressionSyntax>().FirstOrDefault() is { } toes => (true, toes.ToString()),
                        _ => (false, "ERROR ... attribute ctor seems to lack typeof expression.")
                    };
                }
            }
        }

        private (bool Ok, TypeOfExpressionSyntax? TargetType, bool? IsTargetTypeTemplateInterface) ExtractInstantiationInfo(AttributeSyntax matchingAttrib)
        {
            int attribArgCount = matchingAttrib.ArgumentList?.Arguments.Count ?? 0;
            bool matches = matchingAttrib.Name.ToString() == CjmTemplateInstantiationAttribute.ShortName;

            return (matches, attribArgCount, matchingAttrib.ArgumentList) switch
            {
                (true, 2, {} argList) => ExtractFromArgListOfSize2(argList.Arguments),
                _ => (false, null, null),
            };

            static (bool Ok, TypeOfExpressionSyntax? Toes, bool? IsTargetTypeTemplateInterface)
                ExtractFromArgListOfSize2(SeparatedSyntaxList<AttributeArgumentSyntax> items)
            {
                Debug.Assert(items.Count == 2);
                TypeOfExpressionSyntax? toes = items[0].Expression as TypeOfExpressionSyntax;
                bool? isTargetTemplateInterface = items[1].Expression switch
                {
                    LiteralExpressionSyntax expr when expr.Kind() is SyntaxKind.TrueLiteralExpression or SyntaxKind.TrueKeyword => true,
                    LiteralExpressionSyntax expr when expr.Kind() is SyntaxKind.FalseLiteralExpression or SyntaxKind.FalseKeyword => false,
                    _ => null
                };
                return (toes != null && isTargetTemplateInterface != null, toes, isTargetTemplateInterface);
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
                    matchingAttribute = matchingTemplateInterface.First();
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
                        errors = errors || (declaringType is not ClassDeclarationSyntax &&
                            declaringType is not StructDeclarationSyntax) || matchingInstantiation.Length > 1;
                        matchingAttribute = matchingInstantiation.First();
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
                ImmutableArray<FoundTemplateInterfaceRecord>.Builder? templIntrf =
                    Volatile.Read(ref _templateInterfaceRecordsBldr);
                ImmutableArray<FoundTemplateImplementationRecord>.Builder? implTempl =
                    Volatile.Read(ref _tempImplRecordsBldr);
                ImmutableArray<FoundTemplateInstantiationRecord>.Builder? instantTempl =
                    Volatile.Read(ref _tempInstantRecordsBldr);
                {
                    var bldr = Interlocked.Exchange(ref _templateInterfaceRecordsBldr, null);
                    if (bldr == null)
                    {
                        if (templIntrf != null)
                        {
                            Volatile.Write(ref _templateInterfaceRecordsBldr, templIntrf);
                        }
                        _freezeFlag.CancelFreezeOrThrow();
                        throw new LocklessMultiStepException(
                            $"During a freeze operation, builder {nameof(_templateInterfaceRecordsBldr)} found null.");
                    }

                    _templateInterfaceRecords = bldr.ToImmutable();
                }
                {
                    var bldr = Interlocked.Exchange(ref _tempImplRecordsBldr, null);
                    if (bldr == null)
                    {
                        if (implTempl != null)
                        {
                            Volatile.Write(ref _tempImplRecordsBldr, implTempl);
                        }
                        if (templIntrf != null)
                        {
                            Volatile.Write(ref _templateInterfaceRecordsBldr, templIntrf);
                        }
                        _freezeFlag.CancelFreezeOrThrow();
                        throw new LocklessMultiStepException(
                            $"During a freeze operation, builder {nameof(_tempImplRecordsBldr)} unexpectedly found null.");
                    }
                    _templateImplementationRecords = bldr.ToImmutable();
                }
                {
                    var bldr = Interlocked.Exchange(ref _tempInstantRecordsBldr, null);
                    if (bldr == null)
                    {
                        if (implTempl != null)
                        {
                            Volatile.Write(ref _tempImplRecordsBldr, implTempl);
                        }
                        if (templIntrf != null)
                        {
                            Volatile.Write(ref _templateInterfaceRecordsBldr, templIntrf);
                        }
                        _freezeFlag.CancelFreezeOrThrow();
                        throw new LocklessMultiStepException(
                            $"During a freeze operation, builder {nameof(_tempInstantRecordsBldr)} unexpectedly found null.");
                    }
                    _templateInstantRecords = bldr.ToImmutable();
                }

                _freezeFlag.FinishFreezeOrThrow();
            }
            else if (!_freezeFlag.IsFrozen)
            {
                throw new LocklessMultiStepException("Flag is already frozen or is being frozen on another thread.");
            }
        }

        private ImmutableArray<FoundTemplateInterfaceRecord> _templateInterfaceRecords;
        private ImmutableArray<FoundTemplateInterfaceRecord>.Builder? _templateInterfaceRecordsBldr;
        private ImmutableArray<FoundTemplateImplementationRecord> _templateImplementationRecords;
        private ImmutableArray<FoundTemplateImplementationRecord>.Builder? _tempImplRecordsBldr;
        private ImmutableArray<FoundTemplateInstantiationRecord> _templateInstantRecords;
        private ImmutableArray<FoundTemplateInstantiationRecord>.Builder? _tempInstantRecordsBldr;

        private readonly LocklessFreezeFlag _freezeFlag = new();
        
    }
}