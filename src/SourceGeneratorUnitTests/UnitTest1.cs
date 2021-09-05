using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Cjm.CodeGen;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace SourceGeneratorUnitTests
{
    public class GeneratorTests 
    {
        public ITestOutputHelper Helper { get; }
        public const string HelloWorld = "Console.WriteLine(\"Hello, world!\");";
        public const string SimpleSource =
@"
using System;
namespace Cjm.Test
{
    class Program
    {
        public static void Main()
        {
            Console.WriteLine();         
        }
    }
    
    class AnotherProgram
    {
        public int Five => 5;
    }
}
";

        public GeneratorTests(ITestOutputHelper helper)
        {
            AlternateLoggerSource.InjectAlternateLogger(helper ?? throw new ArgumentNullException(nameof(helper)));
            Helper = helper;
        }

        [Fact]
        public void FirstTest()
        {
            //foreach (var assemRef in ReferencesToAssemblies)
            //{
            //    Helper.WriteLine("Assembly Name: \t[{0}].", assemRef.Display ?? "UNKNOWN");
            //}
            Compilation comp = CreateCompilation(SimpleSource);
            var newComp = RunGenerators(comp, out var generatorDiags, new TransformEnumeratorGenerator());

            Assert.Empty(generatorDiags);
            Assert.Empty(newComp.GetDiagnostics());
        }

        private static Compilation CreateCompilation(string source)
            => CSharpCompilation.Create("compilation",
                new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview)) },
                ReferencesToAssemblies,
                new CSharpCompilationOptions(OutputKind.ConsoleApplication));

        private static GeneratorDriver CreateDriver(Compilation c, params ISourceGenerator[] generators)
            =>  CSharpGeneratorDriver.Create(parseOptions: (CSharpParseOptions) c.SyntaxTrees.First().Options,
                generators: ImmutableArray.Create(generators),
                optionsProvider: null,
                additionalTexts: ImmutableArray<AdditionalText>.Empty);

        private static Compilation RunGenerators(Compilation c, out ImmutableArray<Diagnostic> diagnostics, params ISourceGenerator[] generators)
        {
            CreateDriver(c, generators).RunGeneratorsAndUpdateCompilation(c, out var d, out diagnostics);
            return d;
        }

        private static IEnumerable<MetadataReference> GetAllFrameworkReferences()
        {
            AppDomain domain = AppDomain.CurrentDomain;
            var assemblies = domain.GetAssemblies() ?? Array.Empty<Assembly>();
            foreach (var item in assemblies.Where(assem => !assem.IsDynamic))
            {
                yield return MetadataReference.CreateFromFile(item.Location, MetadataReferenceProperties.Assembly);
            }
        }

        private static readonly ImmutableArray<MetadataReference> ReferencesToAssemblies =
            GetAllFrameworkReferences().ToImmutableArray();
    }
}
