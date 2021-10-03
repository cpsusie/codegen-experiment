using System;
using Xunit;
using Xunit.Abstractions;

namespace Cjm.Templates.Test
{
    public abstract class AlternatelyLoggedTests
    {
        protected ITestOutputHelper Helper { get; }
        protected AlternatelyLoggedTests(ITestOutputHelper outputHelper)
        {
            Helper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
            var logger = SourceGeneratorUnitTests.AlternateLoggerSource.CreateAlternateLogger(outputHelper);
            Utilities.LoggerSource.InjectAlternateLoggerOrThrow(logger);
        }
    }

    public abstract class FixtureHavingAlternatelyLoggedTests<TFixture> : AlternatelyLoggedTests, IClassFixture<TFixture>
        where TFixture : class
    {
        protected TFixture Fixture { get; }

        protected FixtureHavingAlternatelyLoggedTests(ITestOutputHelper outputHelper, TFixture fixture) :
            base(outputHelper) => Fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
    }
}