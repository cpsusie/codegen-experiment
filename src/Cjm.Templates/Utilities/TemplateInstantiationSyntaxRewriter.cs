using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
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

            do
            {
                hitCount = 0;
                foreach (var node in currentSyntax.DescendantNodes().OfType<TypeParameterConstraintClauseSyntax>())
                {
                    SyntaxNode? result = VisitTypeParameterConstraintClause(node);
                    if (result == null)
                    {
                        CompilationUnitSyntax?  temp = currentSyntax.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
                        if (temp != null && temp != currentSyntax)
                        {
                            currentSyntax = temp;
                            ++hitCount;
                        }
                    }
                }

            } while (hitCount > 0);

            return currentSyntax;
        }

        public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node) => node == _ndeclSnytax ? node.WithName(_instantiationNamespace.Name) : node;

        public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax syntax)
        {
            SyntaxNode ret = syntax;
            if (_instantiator.ImplData.ImplRecord.DeclaringImplementation is StructDeclarationSyntax sd &&
                sd == syntax)
            {
                var foobar = SyntaxFactory.TokenList(new[]
                {
                    SyntaxFactory.Token(SyntaxKind.PartialKeyword).WithLeadingTrivia(SyntaxFactory.Space)
                        .WithTrailingTrivia(SyntaxFactory.Space)
                });
                ret = syntax.WithIdentifier(_instantiator.InstantiationRecord.InstantiationDeclaration.Identifier.WithLeadingTrivia(SyntaxTriviaList.Create(SyntaxFactory.Space)).WithTrailingTrivia(SyntaxFactory.Space)).WithModifiers(foobar);
            }
            return ret;
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax syntax)
        {
            SyntaxNode ret = syntax;
            if (_instantiator.ImplData.ImplRecord.DeclaringImplementation is ClassDeclarationSyntax cd &&
                cd == syntax)
            {
                var foobar = SyntaxFactory.TokenList(new[] { SyntaxFactory.Token(SyntaxKind.PartialKeyword) });
                ret = syntax.WithIdentifier(_instantiator.InstantiationRecord.InstantiationDeclaration.Identifier).WithModifiers(foobar);
            }
            return ret;
        }

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

        static NamespaceDeclarationSyntax? ExtractNamespaceFromTypeDecl(TypeDeclarationSyntax tds)
        {
            SyntaxNode? parent = tds.Parent;
            while (parent != null && parent is not NamespaceDeclarationSyntax)
            {
                parent = parent.Parent;
            }
            return parent as NamespaceDeclarationSyntax;
        }

        private readonly NamespaceDeclarationSyntax _instantiationNamespace;
        private readonly NamespaceDeclarationSyntax _ndeclSnytax;
        private readonly Instantiator _instantiator;
        private readonly GeneratorExecutionContext _context;
        private readonly SyntaxTokenList _instantiationModifiers;
        private readonly SyntaxTokenList _implementationModifiers;
    }
}
