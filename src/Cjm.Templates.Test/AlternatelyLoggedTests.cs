using System;
using Cjm.Templates.Utilities;
using LoggerLibrary;
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
            
            if (LoggerSource.IsLoggerAlreadySet && LoggerSource.Logger is IWrap<ITestOutputHelper> wrapped)
            {
                wrapped.Update(outputHelper);
                return;
            }

            ICodeGenLogger logger =
                SourceGeneratorUnitTests.AlternateLoggerSource.CreateAlternateLogger(outputHelper);
            try
            {

                LoggerSource.InjectAlternateLoggerOrThrow(logger);
            }
            catch (Exception)
            {
                logger.Dispose();
                if (LoggerSource.Logger is IWrap<ITestOutputHelper> wrapper)
                {
                    wrapper.Update(outputHelper);
                }
                else
                {
                    throw;
                }
            }

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