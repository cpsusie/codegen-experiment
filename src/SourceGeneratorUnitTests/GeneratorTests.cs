using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Cjm.CodeGen;
using Cjm.CodeGen.Attributes;
using HpTimeStamps;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;
using Xunit.Abstractions;

namespace SourceGeneratorUnitTests
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class GeneratorTests 
    {
        public ITestOutputHelper Helper { get; }
        public const string HelloWorld = "Console.WriteLine(\"Hello, world!\");";
        public static readonly string SimpleSource = TestCases.TestCase1;
        public static readonly string OneOfEachPayloadType = TestCases.TestCase2;
        
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
            const int expectedSyntaxPayloads = 0;
            const int expectedSematicPayloads = 0;
            const int expectedFinalPayloads = 0;

            string source = SimpleSource;
            ExecuteTest(expectedSyntaxPayloads, expectedSematicPayloads, expectedFinalPayloads, source, 0, 0,
                ValidateSyntaxPayload, ValidateSemanticPayload, ValidateFinalPayload);

            static void ValidateSyntaxPayload(GeneratorTestEnableAugmentSyntaxReceiverPayloadEventArgs e)
            {
               Assert.NotNull(e);
            }

            static void ValidateSemanticPayload(GeneratorTestEnableAugmentSemanticPayloadEventArgs e)
            {
                Assert.NotNull(e);
            }

            static void ValidateFinalPayload(GeneratorTestingEnableAugmentedEnumerationFinalPayloadEventArgs e)
            {
                Assert.NotNull(e);
            }
        }

        [Fact]
        [SuppressMessage("Assertions", "xUnit2013:Do not use equality check to check for collection size.", Justification = "Oneness of expected count coincidental.")]
        public void TestOneSyntaxReceiverPayload()
        {
            const int expectedSyntaxPayloads = 1;
            const int expectedSematicPayloads = 1;
            const int expectedFinalPayloads = 1;
            string source = OneOfEachPayloadType;
            const string decoratedClassName = "AnotherProgram";

            ExecuteTest(expectedSyntaxPayloads, expectedSematicPayloads, expectedFinalPayloads, source, 0, 0,
                ValidateSyntaxPayload, ValidateSemanticPayload, ValidateFinalPayload);


            static void ValidateSyntaxPayload(GeneratorTestEnableAugmentSyntaxReceiverPayloadEventArgs e)
            {
                ClassDeclarationSyntax cds = e.Payload.ClassToAugment;
                AttributeSyntax attribSyn = e.Payload.AttributeSyntax;
                TypeOfExpressionSyntax targetType = e.Payload.AttributeTargetDataSyntax;
                Assert.NotNull(cds);
                Assert.NotNull(attribSyn);
                Assert.NotNull(targetType);
                Assert.True(cds.Identifier.Value is string txt && string.Equals(txt, decoratedClassName, StringComparison.Ordinal));
                Assert.Contains(cds.Modifiers, mod => mod.Kind() == SyntaxKind.PartialKeyword);
                Assert.Contains(cds.Modifiers, mod => mod.Kind() == SyntaxKind.PublicKeyword);
                Assert.Contains(cds.Modifiers, mod => mod.Kind() == SyntaxKind.StaticKeyword);

                Assert.Equal(EnableAugmentedEnumerationExtensionsAttribute.ShortName, attribSyn.Name.ToString(),
                    StringComparer.Ordinal);
                const string nameOfType = "List<PortableMonotonicStamp>";
                Assert.Equal(nameOfType, targetType.Type.ToString());
            }

            void ValidateSemanticPayload(GeneratorTestEnableAugmentSemanticPayloadEventArgs e)
            {
                 ref readonly var attribTargetData = ref e.Payload.AttributeTargetData;
                 Assert.NotNull(attribTargetData.AttributeTypeSymbol);

            }

            void ValidateFinalPayload(GeneratorTestingEnableAugmentedEnumerationFinalPayloadEventArgs e)
            {
                var collection = e.Payload;
                Assert.Single(collection);
                Assert.Single(collection.Single().Value);
                Helper.WriteLine(e.ToString());
            }
        }
        

        [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local")]
        private void ExecuteTest(int expectedSyntaxPayloads, int expectedSemanticPayloads, int expectedFinalPayloads,
            string source, int expectedDiagnostics, int expectedNewCompDx,
            Action<GeneratorTestEnableAugmentSyntaxReceiverPayloadEventArgs> syntaxPayloadValidator,
            Action<GeneratorTestEnableAugmentSemanticPayloadEventArgs> semanticPayloadValidator,
            Action<GeneratorTestingEnableAugmentedEnumerationFinalPayloadEventArgs> finalValidator)
        {
            Assert.True(expectedSyntaxPayloads > -1);
            Assert.True(expectedSemanticPayloads > -1);
            Assert.True(expectedFinalPayloads > -1);
            Assert.True(expectedDiagnostics> -1);
            Assert.True(expectedNewCompDx > -1);
            Assert.False(string.IsNullOrWhiteSpace(source));
            Assert.NotNull(syntaxPayloadValidator);
            Assert.NotNull(semanticPayloadValidator);
            Assert.NotNull(finalValidator);


            Compilation comp = CreateCompilation(source);
            Compilation newComp;
            SortedSet<GeneratorTestingPayloadEventArgs> payloadArgs = new();
            ImmutableArray<Diagnostic> diagnostics;
            {
                using var generator = new TransformEnumeratorGenerator();
                generator.MatchingSyntaxDetected += Generator_PayloadFound;
                generator.SemanticPayloadFound += Generator_PayloadFound;
                generator.FinalPayloadCreated += Generator_PayloadFound;

                newComp = RunGenerators(comp, out diagnostics, generator);
            }
            
            ValidateCollectionLength(diagnostics, expectedDiagnostics);
            ValidateCollectionLength(newComp.GetDiagnostics(), expectedNewCompDx);


            var syntaxPayloads = payloadArgs.OfType<GeneratorTestEnableAugmentSyntaxReceiverPayloadEventArgs>().ToImmutableArray();
            var semanticPayloads = payloadArgs.OfType<GeneratorTestEnableAugmentSemanticPayloadEventArgs>().ToImmutableArray();
            var finalPayloads = payloadArgs.OfType<GeneratorTestingEnableAugmentedEnumerationFinalPayloadEventArgs>()
                .ToImmutableArray();

            ValidateCollectionLength(syntaxPayloads, expectedSyntaxPayloads);
            ValidateCollectionLength(semanticPayloads, expectedSemanticPayloads);
            ValidateCollectionLength(finalPayloads, expectedFinalPayloads);

            
            syntaxPayloads.ApplyToAll(syntaxPayloadValidator);
            semanticPayloads.ApplyToAll(semanticPayloadValidator);
            finalPayloads.ApplyToAll(finalValidator);


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void ValidateCollectionLength<T>(ImmutableArray<T> validateMe, int expectedLength)
            {
                Assert.False(validateMe.IsDefault);
                switch (expectedLength)
                {
                    case < 0: 
                        Assert.False(true, "Expected length shouldn't be negative.");
                        break;
                    case 0:
                        Assert.Empty(validateMe);
                        break;
                    case 1:
                        Assert.Single(validateMe);
                        break;
                    default:
                        Assert.Equal(expectedLength, validateMe.Length);
                        break;
                }
            }

            void Generator_PayloadFound(object? o, GeneratorTestingPayloadEventArgs e) => payloadArgs.Add(e);
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
            var assemblies = domain.GetAssemblies();
            foreach (var item in assemblies.Where(assem => !assem.IsDynamic))
            {
                yield return MetadataReference.CreateFromFile(item.Location, MetadataReferenceProperties.Assembly);
            }
        }

        private static readonly ImmutableArray<MetadataReference> ReferencesToAssemblies =
            GetAllFrameworkReferences().Union(GetCodeGenLibAndItsReferences()).ToImmutableArray();
    }


}
