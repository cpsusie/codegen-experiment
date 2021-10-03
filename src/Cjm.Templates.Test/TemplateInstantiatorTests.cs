using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Cjm.CodeGen;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using Xunit.Abstractions;

namespace Cjm.Templates.Test
{
    public class TemplateInstantiatorTests : FixtureHavingAlternatelyLoggedTests<TemplateInterfaceDetectionFixture>
    {
        /// <inheritdoc />
        public TemplateInstantiatorTests(ITestOutputHelper outputHelper, TemplateInterfaceDetectionFixture fixture) : base(outputHelper, fixture)
        {
        }

        [Fact]
        public void TestFixture()
        {
            Assert.Equal(2, Fixture.Lookup.Count);
        }

        [Fact]
        public void TestNoHitCase()
        {
            TemplateInterfaceExpectedResults x = Fixture.Lookup[TemplateInterfaceTestCaseIdentifier.NoHitCase];
            string code = x.Code;
            Assert.NotEmpty(code);
            Assert.NotNull(code);

            Compilation comp = CreateCompilation(code);
            Compilation newComp;
            ImmutableArray<Diagnostic> diagnostics;
            ImmutableHashSet<FoundTemplateInterfaceRecord> finalSet;
            var expectedSyncObj = new object();
            {
                
                var bldr = ImmutableHashSet.CreateBuilder<FoundTemplateInterfaceRecord>();
                {
                    using var generator = new TemplateInstantiator();
                    generator.TemplateInterfaceRecordsFound +=
                        (sender, e) =>
                        {
                            lock (expectedSyncObj)
                            {
                                bldr.UnionWith(e.IdentifiedTemplateInterfaceRecords);
                            }
                        };
                    newComp = RunGenerators(comp, out diagnostics, generator);
                    Thread.Sleep(TimeSpan.FromSeconds(0.5));

                }
                lock (expectedSyncObj)
                {
                    finalSet = bldr.ToImmutable();
                }
            }
            Assert.NotNull(newComp);
            Assert.Equal(x.ExpectedNumberHits, finalSet.Count);
            Assert.True(diagnostics.Count(dx => dx.Severity >= DiagnosticSeverity.Warning) == 0);

        }

        [Fact]
        public void TestHitCase()
        {
            TemplateInterfaceExpectedResults x = Fixture.Lookup[TemplateInterfaceTestCaseIdentifier.EnumComparer];
            string code = x.Code;
            Assert.NotEmpty(code);
            Assert.NotNull(code);

            Compilation comp = CreateCompilation(code);
            Compilation newComp;
            ImmutableArray<Diagnostic> diagnostics;
            ImmutableHashSet<FoundTemplateInterfaceRecord> finalSet;
            var expectedSyncObj = new object();
            {

                var bldr = ImmutableHashSet.CreateBuilder<FoundTemplateInterfaceRecord>();
                {
                    using var generator = new TemplateInstantiator();
                    generator.TemplateInterfaceRecordsFound +=
                        (sender, e) =>
                        {
                            lock (expectedSyncObj)
                            {
                                bldr.UnionWith(e.IdentifiedTemplateInterfaceRecords);
                            }
                        };
                    newComp = RunGenerators(comp, out diagnostics, generator);
                    Thread.Sleep(TimeSpan.FromSeconds(0.5));

                }
                lock (expectedSyncObj)
                {
                    finalSet = bldr.ToImmutable();
                }
            }
            Assert.NotNull(newComp);
            Assert.Equal(x.ExpectedNumberHits, finalSet.Count);
            Assert.True(diagnostics.Count(dx => dx.Severity >= DiagnosticSeverity.Warning) == 0);
        }

        

        private static Compilation CreateCompilation(string source)
           => CSharpCompilation.Create("compilation",
               new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview)) },
               ReferencesToAssemblies,
               new CSharpCompilationOptions(OutputKind.ConsoleApplication));

        private static GeneratorDriver CreateDriver(Compilation c, params ISourceGenerator[] generators)
            => CSharpGeneratorDriver.Create(parseOptions: (CSharpParseOptions)c.SyntaxTrees.First().Options,
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
