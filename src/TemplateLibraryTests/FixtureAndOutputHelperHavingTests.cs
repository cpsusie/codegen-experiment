using System;
using Xunit;
using Xunit.Abstractions;


namespace TemplateLibraryTests
{
    public abstract class FixtureAndOutputHelperHavingTests<T> where T : CjmTestFixture, IClassFixture<T>
    {
        public ITestOutputHelper Helper { get; }

        public T Fixture { get; }

        protected FixtureAndOutputHelperHavingTests(ITestOutputHelper helper, T fixture)
        {
            Helper = helper ?? throw new ArgumentNullException(nameof(helper));
            Fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        }
    }
}
