using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Cjm.CodeGen;
using Cjm.CodeGen.Attributes;
using HpTimeStamps;
using Xunit;
using Xunit.Abstractions;

namespace SourceGeneratorUnitTests
{
    public class GeneratorTests 
    {
        public ITestOutputHelper Helper { get; }
        public const string HelloWorld = "Console.WriteLine(\"Hello, world!\");";
        public static readonly string SimpleSource = TestCases.TestCase1;
        public static readonly string OnePositiveSyntaxReceiverPayload = TestCases.TestCase2;
        
        public GeneratorTests(ITestOutputHelper helper)
        {
            var now = MonotonicTimeStampUtil<MonotonicStampContext>.StampNow;
            var sum = Duration.FromSeconds(2) + now;
            Assert.True(sum - now == Duration.FromSeconds(2));
            AlternateLoggerSource.InjectAlternateLogger(helper ?? throw new ArgumentNullException(nameof(helper)));
            Helper = helper;
        }

        [Fact]
        public void BasicTestNoResults()
        {
            string source = SimpleSource;
            Compilation comp = CreateCompilation(source);
            Compilation newComp;
            SortedSet<GeneratorTestingPayloadEventArgs> payloadArgs = new SortedSet<GeneratorTestingPayloadEventArgs>();
            ImmutableArray<Diagnostic> diagnostics;
            {
                using var generator = new TransformEnumeratorGenerator();
                generator.MatchingSyntaxDetected += (o, e) =>
                {
                    Debug.Assert(e != null);
                    payloadArgs.Add(e);
                };
                newComp = RunGenerators(comp, out diagnostics, generator);
            }
            Assert.Empty(diagnostics);
            Assert.Empty(newComp.GetDiagnostics());
            Assert.Empty(payloadArgs);
        }

        [Fact]
        public void TestOneSyntaxReceiverPayload()
        {
            string source = OnePositiveSyntaxReceiverPayload;
            const string decoratedClassName = "AnotherProgram";
            Compilation comp = CreateCompilation(source);
            Compilation newComp;
            SortedSet<GeneratorTestingPayloadEventArgs> payloadArgs = new SortedSet<GeneratorTestingPayloadEventArgs>();
            ImmutableArray<Diagnostic> diagnostics;
            {
                using var generator = new TransformEnumeratorGenerator();
                generator.MatchingSyntaxDetected += (o, e) =>
                {
                    Debug.Assert(e != null);
                    payloadArgs.Add(e);
                };
                newComp = RunGenerators(comp, out diagnostics, generator);
            }
            Assert.Empty(diagnostics);
            Assert.Empty(newComp.GetDiagnostics());

            GeneratorTestEnableAugmentSyntaxReceiverPayloadEventArgs singleArg = (GeneratorTestEnableAugmentSyntaxReceiverPayloadEventArgs) payloadArgs.Single();
            Helper.WriteLine("Single argument: ");
            Helper.WriteLine(" \t\t" +singleArg.ToString());
            Helper.WriteLine("Done single argument.");

            Assert.True(singleArg.Payload.ClassToAugment.Identifier.Value is string txt && string.Equals(txt, decoratedClassName, StringComparison.Ordinal));
            Assert.Contains(singleArg.Payload.ClassToAugment.Modifiers, mod => mod.Kind() == SyntaxKind.PartialKeyword);
            Assert.Contains(singleArg.Payload.ClassToAugment.Modifiers, mod => mod.Kind() == SyntaxKind.PublicKeyword);
            Assert.Contains(singleArg.Payload.ClassToAugment.Modifiers, mod => mod.Kind() == SyntaxKind.StaticKeyword);
            Assert.Equal(EnableAugmentedEnumerationExtensionsAttribute.ShortName,
                singleArg.Payload.AttributeSyntax.Name.ToString(), StringComparer.Ordinal);
            const string nameOfType = "List<PortableMonotonicStamp>";
            Assert.Equal(nameOfType, singleArg.Payload.AttributeTargetDataSyntax.Type.ToString());
            
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

        private static IEnumerable<MetadataReference> GetCodeGenLibAndItsReferences()
        {
            var assembly = typeof(TransformEnumeratorGenerator).Assembly;
            yield return MetadataReference.CreateFromFile(assembly.Location, MetadataReferenceProperties.Assembly);
            
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
            GetAllFrameworkReferences().Union(GetCodeGenLibAndItsReferences()).ToImmutableArray();
    }
}
