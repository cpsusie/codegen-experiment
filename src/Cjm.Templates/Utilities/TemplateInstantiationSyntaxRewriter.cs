using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Cjm.Templates.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
#nullable enable
namespace Cjm.Templates.Utilities
{
    internal sealed class TemplateInstantiationSyntaxRewriter : CSharpSyntaxRewriter
    {

        public TemplateInstantiationSyntaxRewriter(Instantiator instantiator, GeneratorExecutionContext context) 
            : this(instantiator, context, false) {}

        /// <inheritdoc />
        public TemplateInstantiationSyntaxRewriter(Instantiator instantiator, GeneratorExecutionContext context,
            bool visitIntoStructuredTrivia) : base(visitIntoStructuredTrivia)
        {
            _instantiator = instantiator ?? throw new ArgumentNullException(nameof(instantiator));
            _context = context;
            _ndeclSnytax = ExtractNamespaceFromTypeDecl(_instantiator.ImplData.ImplRecord.DeclaringImplementation) ??
                           throw new MissingNamespaceException(
                               _instantiator.ImplData.ImplRecord.DeclaringImplementation,
                               _instantiator.ImplData.ImplRecord.ImplementationName);
            _instantiationNamespace =
                ExtractNamespaceFromTypeDecl(_instantiator.InstantiationRecord.InstantiationDeclaration) ??
                throw new MissingNamespaceException(_instantiator.InstantiationRecord.InstantiationDeclaration,
                    _instantiator.InstantiationRecord.InstantiationName);
            _instantiationModifiers = _instantiator.InstantiationRecord.InstantiationDeclaration.Modifiers;
            _implementationModifiers = _instantiator.ImplData.ImplRecord.DeclaringImplementation.Modifiers;

        }

        public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax syntax)
        {
            CompilationUnitSyntax currentSyntax = syntax;
            int hitCount;
            do
            {
                hitCount = 0;
                foreach (var node in currentSyntax.DescendantNodes().OfType<NamespaceDeclarationSyntax>())
                {
                    SyntaxNode result = VisitNamespaceDeclaration(node);
                    if (result != node)
                    {
                        currentSyntax = currentSyntax.ReplaceNode(node, result);
                        ++hitCount;
                    }
                }
            } while (hitCount > 0);

            do
            {
                hitCount = 0;
                foreach (var node in currentSyntax.DescendantNodes().OfType<TypeParameterConstraintClauseSyntax>())
                {
                    SyntaxNode? result = VisitTypeParameterConstraintClause(node);
                    if (result == null)
                    {
                        CompilationUnitSyntax? temp = currentSyntax.RemoveNode(node, SyntaxRemoveOptions.KeepEndOfLine);
                        if (temp != null && temp != currentSyntax)
                        {
                            currentSyntax = temp;
                            ++hitCount;
                        }
                    }
                }

            } while (hitCount > 0);

            _context.CancellationToken.ThrowIfCancellationRequested();
            do
            {
                hitCount = 0;
                foreach (var node in currentSyntax.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    SyntaxNode result = VisitClassDeclaration(node);
                    if (result != node)
                    {
                        currentSyntax = currentSyntax.ReplaceNode(node, result);
                        ++hitCount;
                    }
                }

            } while (hitCount > 0);
            _context.CancellationToken.ThrowIfCancellationRequested();
            do
            {
                hitCount = 0;
                foreach (var node in currentSyntax.DescendantNodes().OfType<StructDeclarationSyntax>())
                {
                    SyntaxNode result = VisitStructDeclaration(node);
                    if (result != node)
                    {
                        currentSyntax = currentSyntax.ReplaceNode(node, result);
                        ++hitCount;
                    }
                }

            } while (hitCount > 0);
            _context.CancellationToken.ThrowIfCancellationRequested();



            return currentSyntax;
        }

        public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node) => node == _ndeclSnytax ? node.WithName(_instantiationNamespace.Name) : node;

        public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax syntax) =>
            VisitTypeDeclarationSyntax(syntax);


        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax syntax) =>
            VisitTypeDeclarationSyntax(syntax);

        public override SyntaxNode? VisitTypeParameterConstraintClause(TypeParameterConstraintClauseSyntax syntax)
        {
            ImmutableArrayByRefAdapter<SubPair> x = _instantiator.SubstitutionPairs;
            foreach (ref readonly var i in x)
            {
                if (i.ToBeReplaced.Identifier.Text == syntax.Name.Identifier.Text)
                    return null;
            }
            return syntax;
        }

        public override SyntaxNode? VisitAttributeList(AttributeListSyntax als)
        {
            int totalItems = als.Attributes.Count;
            ImmutableHashSet<AttributeSyntax> syntax;
            {
                var bldr = ImmutableHashSet.CreateBuilder<AttributeSyntax>();
                foreach (var attribute in als.Attributes)
                {
                    SyntaxNode? temp = VisitAttribute(attribute);
                    if (temp == null)
                    {
                        bldr.Add(attribute);
                    }
                }
                syntax = bldr.ToImmutable();
            }

            Debug.Assert(totalItems >= syntax.Count);

            return (totalItems - syntax.Count) switch
            {
                <= 0 => null,
                _ => als.RemoveNodes(syntax, SyntaxRemoveOptions.KeepExteriorTrivia)
            };
            
        }

        public override SyntaxNode? VisitAttribute(AttributeSyntax attrib) =>
            attrib.ChildNodes().OfType<IdentifierNameSyntax>().FirstOrDefault() is 
            { Identifier: { Text: nameof(CjmTemplateImplementationAttribute)
                or CjmTemplateImplementationAttribute.ShortName
            } } ? null : attrib;


        static NamespaceDeclarationSyntax? ExtractNamespaceFromTypeDecl(TypeDeclarationSyntax tds)
        {
            SyntaxNode? parent = tds.Parent;
            while (parent != null && parent is not NamespaceDeclarationSyntax)
            {
                parent = parent.Parent;
            }
            return parent as NamespaceDeclarationSyntax;
        }

        private SyntaxNode? RemoveTypeParameterListFromTypeDeclaration(TypeParameterListSyntax tpls)
        {
            if (tpls.Parameters.Any())
            {
                return null;
            }

            return tpls;
        }

        private SyntaxNode VisitTypeDeclarationSyntax(TypeDeclarationSyntax syntax)
        {
            TypeDeclarationSyntax ret = syntax;
            if (_instantiator.ImplData.ImplRecord.DeclaringImplementation is { } td &&
                td.Identifier.Text == syntax.Identifier.Text)
            {
                var leadingTrivia = td.Modifiers.Any() ? td.Modifiers.First().LeadingTrivia : td.Keyword.LeadingTrivia;
                var foobar = SyntaxFactory.TokenList(new[]
                {
                    SyntaxFactory.Token(SyntaxKind.PartialKeyword).WithLeadingTrivia(leadingTrivia)
                        .WithTrailingTrivia(SyntaxFactory.Space)
                });
                if (syntax.TypeParameterList != null)
                {
                    if (RemoveTypeParameterListFromTypeDeclaration(syntax.TypeParameterList) == null)
                    {
                        TypeDeclarationSyntax? temp = syntax.RemoveNode(syntax.TypeParameterList!, SyntaxRemoveOptions.KeepTrailingTrivia);
                        if (temp != null)
                        {
                            syntax = temp;
                        }
                    }
                }
                ret = syntax
                    .WithIdentifier(_instantiator.InstantiationRecord.InstantiationDeclaration.Identifier
                        .WithLeadingTrivia(SyntaxTriviaList.Create(SyntaxFactory.Space))
                        .WithTrailingTrivia(SyntaxFactory.Space)).WithModifiers(foobar);
                _context.CancellationToken.ThrowIfCancellationRequested();

                ImmutableArray<IdentifierNameSyntax> inses = ret.DescendantNodes().OfType<IdentifierNameSyntax>()
                    .ToImmutableArray();
                {
                    var bldr = ImmutableDictionary
                        .CreateBuilder<IdentifierNameSyntax, IdentifierNameSyntax>();

                    foreach (IdentifierNameSyntax ins in inses)
                    {
                        var temp = VisitIdentifierName(ins);
                        if (temp != ins && temp is IdentifierNameSyntax tempIns)
                        {
                            bldr.Add(ins, tempIns);
                        }
                    }

                    var subs = bldr.ToImmutable();
                    if (subs.Any())
                    {
                        _context.CancellationToken.ThrowIfCancellationRequested();
                        ret = ret.ReplaceNodes(subs.Keys, (o, _) => subs[o]);
                    }
                }

                _context.CancellationToken.ThrowIfCancellationRequested();
                
                {
                    ImmutableArray<AttributeListSyntax> attribListsToRemove = (from attribute in ret.AttributeLists
                        let visited = VisitAttributeList(attribute)
                        where visited == null
                        select attribute).ToImmutableArray();

                    var temp = ret.RemoveNodes(attribListsToRemove, SyntaxRemoveOptions.KeepExteriorTrivia);
                    if (temp != null && temp != ret)
                    {
                        ret = temp;
                    }
                }
                _context.CancellationToken.ThrowIfCancellationRequested();
                {
                    ImmutableDictionary<AttributeListSyntax, AttributeListSyntax> attribSwapLookup =
                        (from attributeList in ret.AttributeLists
                            let visited = VisitAttributeList(attributeList) as AttributeListSyntax
                            where visited != null && visited != attributeList
                            select new KeyValuePair<AttributeListSyntax, AttributeListSyntax>(attributeList, visited))
                        .ToImmutableDictionary();
                    if (attribSwapLookup.Any())
                    {
                        ret = ret.ReplaceNodes(attribSwapLookup.Keys, (o, _) => attribSwapLookup[o]);
                    }
                }
            }
            return ret;
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax ins)
        {
            IdentifierNameSyntax ret = ins;
            foreach (ref readonly var item in _instantiator.SubstitutionPairs)
            {
                if (item.ToBeReplaced.Identifier.Text == ins.Identifier.Text)
                {
                    ret = ins.WithIdentifier(SyntaxFactory.Identifier(ins.GetLeadingTrivia(), item.ReplaceWithMe.Name,
                        ins.GetTrailingTrivia()));
                    return ret;
                }
            }
            return ret;
        }

        private readonly NamespaceDeclarationSyntax _instantiationNamespace;
        private readonly NamespaceDeclarationSyntax _ndeclSnytax;
        private readonly Instantiator _instantiator;
        private readonly GeneratorExecutionContext _context;
        private readonly SyntaxTokenList _instantiationModifiers;
        private readonly SyntaxTokenList _implementationModifiers;
    }
}
