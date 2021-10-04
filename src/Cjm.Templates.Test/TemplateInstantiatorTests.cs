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
            Assert.Equal(4, Fixture.Lookup.Count);
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
                        (_, e) =>
                        {
                            lock (expectedSyncObj)
                            {
                                bldr.UnionWith(e.IdentifiedTemplateRecords);
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
                        (_, e) =>
                        {
                            lock (expectedSyncObj)
                            {
                                bldr.UnionWith(e.IdentifiedTemplateRecords);
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
            Assert.True(x.FoundNames.SetEquals(finalSet.Select(r => r.TemplateName)));
        }

        [Fact]
        public void TestEnumComparerImpl()
        {
            TemplateInterfaceExpectedResults x = Fixture.Lookup[TemplateInterfaceTestCaseIdentifier.EnumComparerImpl];
            string code = x.Code;
            Assert.NotEmpty(code);
            Assert.NotNull(code);

            Compilation comp = CreateCompilation(code);
            Compilation newComp;
            ImmutableArray<Diagnostic> diagnostics;
            ImmutableHashSet<FoundTemplateInterfaceRecord> finalInterfaceSet;
            ImmutableHashSet<FoundTemplateImplementationRecord> finalImplementationSet;

            var expectedInterfSyncObj = new object();
            var expectedImplSyncObject = new object();
            {
                
                var interfBldr = ImmutableHashSet.CreateBuilder<FoundTemplateInterfaceRecord>();
                var implBldr = ImmutableHashSet.CreateBuilder<FoundTemplateImplementationRecord>();
                {
                    using var generator = new TemplateInstantiator();
                    generator.TemplateInterfaceRecordsFound +=
                        (_, e) =>
                        {
                            lock (expectedInterfSyncObj)
                            {
                                interfBldr.UnionWith(e.IdentifiedTemplateRecords);
                            }
                        };
                        generator.TemplateImplementationRecordsFound+=
                        (_, e) =>
                        {
                            lock (expectedImplSyncObject)
                            {
                                implBldr.UnionWith(e.IdentifiedTemplateRecords);
                            }
                        };
                    newComp = RunGenerators(comp, out diagnostics, generator);
                    Thread.Sleep(TimeSpan.FromSeconds(0.5));
                }
                lock (expectedInterfSyncObj)
                {
                    finalInterfaceSet = interfBldr.ToImmutable();
                    finalImplementationSet = implBldr.ToImmutable();
                }
            }

            Assert.NotNull(newComp);
            Assert.Equal(x.ExpectedNumberHits, finalInterfaceSet.Count + finalImplementationSet.Count);
            Assert.True(diagnostics.Count(dx => dx.Severity >= DiagnosticSeverity.Warning) == 0);
            Assert.True(x.FoundNames.SetEquals(finalImplementationSet.Select(v => v.ImplementationName)));
        }

        [Fact]
        public void TestEnumInstant()
        {
            TemplateInterfaceExpectedResults x = Fixture.Lookup[TemplateInterfaceTestCaseIdentifier.EnumComparerInstant];
            string code = x.Code;
            Assert.NotEmpty(code);
            Assert.NotNull(code);

            Compilation comp = CreateCompilation(code);
            Compilation newComp;
            ImmutableArray<Diagnostic> diagnostics;
            ImmutableHashSet<FoundTemplateInterfaceRecord> finalInterfaceSet;
            ImmutableHashSet<FoundTemplateImplementationRecord> finalImplementationSet;
            ImmutableHashSet<FoundTemplateInstantiationRecord> finalInstantiationSet;

            var expectedInterfSyncObj = new object();
            var expectedImplSyncObject = new object();
            var expectedInstantSyncObject = new object();

            {

                var interfBldr = ImmutableHashSet.CreateBuilder<FoundTemplateInterfaceRecord>();
                var implBldr = ImmutableHashSet.CreateBuilder<FoundTemplateImplementationRecord>();
                var instantBldr = ImmutableHashSet.CreateBuilder<FoundTemplateInstantiationRecord>();
                {
                    using var generator = new TemplateInstantiator();
                    generator.TemplateInterfaceRecordsFound +=
                        (_, e) =>
                        {
                            lock (expectedInterfSyncObj)
                            {
                                interfBldr.UnionWith(e.IdentifiedTemplateRecords);
                            }
                        };
                    generator.TemplateImplementationRecordsFound +=
                        (_, e) =>
                        {
                            lock (expectedImplSyncObject)
                            {
                                implBldr.UnionWith(e.IdentifiedTemplateRecords);
                            }
                        };
                    generator.TemplateInstantiationRecordsFound += (_, e) =>
                    {
                        lock (expectedInstantSyncObject)
                        {
                            instantBldr.UnionWith(e.IdentifiedTemplateRecords);
                        }
                    };
                    newComp = RunGenerators(comp, out diagnostics, generator);
                    Thread.Sleep(TimeSpan.FromSeconds(0.5));
                }
                lock (expectedInterfSyncObj)
                {
                    finalInterfaceSet = interfBldr.ToImmutable();
                    finalImplementationSet = implBldr.ToImmutable();
                    finalInstantiationSet = instantBldr.ToImmutable();
                }
            }

            Assert.NotNull(newComp);
            Assert.Equal(x.ExpectedNumberHits, finalInterfaceSet.Count + finalImplementationSet.Count + finalInstantiationSet.Count);
            Assert.True(diagnostics.Count(dx => dx.Severity >= DiagnosticSeverity.Warning) == 0);
            Assert.True(x.FoundNames.SetEquals(finalInstantiationSet.Select(v => v.InstantiationName)));

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
