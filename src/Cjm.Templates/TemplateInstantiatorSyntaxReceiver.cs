using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Cjm.Templates.Attributes;
using Cjm.Templates.Utilities;
using Cjm.Templates.Utilities.SetOnce;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cjm.Templates
{
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
            if (matchingAttrib != null && extractionResult.IsInterfaceSpecific() && declaredType != null )
            {
                string name = declaredType.Identifier.ToString();
                AddToIfClear(Volatile.Read(ref _templateInterfaceRecordsBldr), 
                    new FoundTemplateInterfaceRecord(name, declaredType ?? throw new ArgumentNullException(nameof(declaredType)), matchingAttrib));
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
}