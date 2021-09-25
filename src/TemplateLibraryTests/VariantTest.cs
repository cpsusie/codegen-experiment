using System;
using Cjm.Templates.Attributes;
using HpTimeStamps;
using Xunit;
using Xunit.Abstractions;

namespace TemplateLibraryTests
{
    public delegate bool PdComparer(in PortableDuration l, in PortableDuration r);
    public class VariantTest 
    {
        public ITestOutputHelper Helper { get; }

        public VariantTest(ITestOutputHelper helper) =>
            Helper = helper ?? throw new ArgumentNullException(nameof(helper));

        [Fact]
        public void TestVariant()
        {
            ReferenceTypeConstraint sealedConstraint = ReferenceTypeConstraint.MustBeImmutableAndSealed;
            DelegateReferenceTypeConstraint portableEqComDel = DelegateReferenceTypeConstraint.CreateSpecificDelegateTypeConstraint(typeof(PdComparer));
            ReferenceTypeConstraintVariant variantEmpty = default;
            ReferenceTypeConstraintVariant variantDelEq = portableEqComDel;
            ReferenceTypeConstraintVariant variantSldImmut = sealedConstraint;

            Assert.True(variantEmpty.IsEmptyOrInvalid);
            Assert.False(variantEmpty.IsDelegateConstraint);
            Assert.False(variantEmpty.IsRefBaseConstraint);

            Assert.False(variantDelEq.IsEmptyOrInvalid);
            Assert.True(variantDelEq.IsDelegateConstraint);
            Assert.False(variantDelEq.IsRefBaseConstraint);

            Assert.False(variantSldImmut.IsEmptyOrInvalid);
            Assert.False(variantSldImmut.IsDelegateConstraint);
            Assert.True(variantSldImmut.IsRefBaseConstraint);

            Assert.False(variantEmpty == variantDelEq);
            Assert.False(variantDelEq == variantSldImmut);
            Assert.False(variantSldImmut == variantEmpty);

            Assert.True(variantSldImmut == sealedConstraint);
            Assert.True(variantDelEq == portableEqComDel);

            ReferenceTypeConstraint rtSealed = (ReferenceTypeConstraint)variantSldImmut;
            DelegateReferenceTypeConstraint rtDel = (DelegateReferenceTypeConstraint) variantDelEq;
            
            Assert.True(rtSealed == sealedConstraint);
            Assert.True(rtSealed.GetHashCode() == sealedConstraint.GetHashCode());
            Assert.True(rtDel == portableEqComDel);
            Assert.True(rtDel.GetHashCode() == portableEqComDel.GetHashCode());

            ReferenceTypeConstraintVariant finalEmpty = default;
            ReferenceTypeConstraintVariant finalDelEq = portableEqComDel;
            ReferenceTypeConstraintVariant finalSldImmut = sealedConstraint;

            Assert.True(finalEmpty == variantEmpty);
            Assert.True(finalEmpty.GetHashCode() == variantEmpty.GetHashCode());

            Assert.True(finalDelEq== variantDelEq);
            Assert.True(finalDelEq.GetHashCode() == variantDelEq.GetHashCode());

            Assert.True(finalSldImmut== variantSldImmut);
            Assert.True(finalSldImmut.GetHashCode() == variantSldImmut.GetHashCode());


        }
    }

    public sealed class VariantTestFixture : CjmTestFixture, IClassFixture<VariantTestFixture>
    {

    }
}
