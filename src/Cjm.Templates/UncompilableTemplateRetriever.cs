using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Cjm.Templates.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

#nullable enable 
namespace Cjm.Templates
{
    public record struct UncompilableTemplateData
        (string FileName, string TemplateCode, CompilationUnitSyntax? SyntaxTree);
    


    internal readonly struct UncompilableTemplateRetriever
    {
        public ImmutableSortedDictionary<string, UncompilableTemplateData> RetrieveTemplates()
        {
            var bldr = ImmutableSortedDictionary.CreateBuilder<string, UncompilableTemplateData>();
            int count = 1;
            foreach (string src in AdditionalTemplatesRepository.AdditionalTemplates)
            {
                string name = count.ToString();

                CompilationUnitSyntax? syntaxTree = CreateAstForCompUnit(src, name);

                bldr.Add(name, new UncompilableTemplateData(name, src, syntaxTree));
                ++count;
            }
            return bldr.ToImmutable();
        }

        CompilationUnitSyntax? CreateAstForCompUnit(string src, string name)
        {
            CompilationUnitSyntax? ret;
            try
            {
                SyntaxTree tree = CSharpSyntaxTree.ParseText(src,
                    new CSharpParseOptions(_version));
                _token.ThrowIfCancellationRequested();
                ret = (CompilationUnitSyntax)tree.GetRoot();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                TraceLog.LogException(e);
                TraceLog.LogError(
                    $"Exception: Unable to create an abstract syntax tree for file {name} with text:{Environment.NewLine}{src}");
                ret = null;
            }
            return ret;
        }

        public UncompilableTemplateRetriever(GeneratorPostInitializationContext context, LanguageVersion v = LanguageVersion.Preview)
        {
            _token = context.CancellationToken;
            _version = v;
        }

        private readonly CancellationToken _token = CancellationToken.None;
        private readonly LanguageVersion _version = LanguageVersion.Preview;
    }
}
