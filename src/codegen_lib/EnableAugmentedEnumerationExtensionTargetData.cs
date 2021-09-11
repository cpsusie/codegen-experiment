using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cjm.CodeGen
{
    public readonly struct EnableAugmentedEnumerationExtensionTargetData : ITargetData,
        IEquatable<EnableAugmentedEnumerationExtensionTargetData>, IHasGenericByRefRoEqComparer<EnableAugmentedEnumerationExtensionTargetData.EqComp, EnableAugmentedEnumerationExtensionTargetData>
    {
        public static EnableAugmentedEnumerationExtensionTargetData CreateTargetData(TypeOfExpressionSyntax tos,
            ClassDeclarationSyntax cds, AttributeSyntax ats) => new(
            tos ?? throw new ArgumentNullException(nameof(tos)), cds ?? throw new ArgumentNullException(nameof(cds)),
            ats ?? throw new ArgumentNullException(nameof(ats)));

        public TypeOfExpressionSyntax AttributeTargetDataSyntax { get; }
        public ClassDeclarationSyntax ClassToAugment => _base.ClassToAugment;
        public AttributeSyntax AttributeSyntax => _base.AttributeSyntax;

        private EnableAugmentedEnumerationExtensionTargetData(TypeOfExpressionSyntax tps,
            ClassDeclarationSyntax cds, AttributeSyntax ats)
        {
            _base = EnableFastLinkExtensionsTargetData.CreateFastLinkExtensionsTargetData(cds, ats);
            AttributeTargetDataSyntax = tps;
        }

        public static bool operator ==(in EnableAugmentedEnumerationExtensionTargetData lhs,
            in EnableAugmentedEnumerationExtensionTargetData rhs) => lhs._base == rhs._base &&
                                                                     TheTpsComparer.Equals(
                                                                         lhs.AttributeTargetDataSyntax,
                                                                         rhs.AttributeTargetDataSyntax);

        public static bool operator !=(in EnableAugmentedEnumerationExtensionTargetData lhs,
            in EnableAugmentedEnumerationExtensionTargetData rhs) => !(lhs == rhs);

        public override int GetHashCode()
        {
            int hash = TheTpsComparer.GetHashCode(AttributeTargetDataSyntax);
            unchecked
            {
                hash = (hash * 397) ^ _base.GetHashCode();
            }
            return hash;
        }

        /// <inheritdoc />
        public EqComp GetComparer() => default;
       

        /// <inheritdoc />
        public override bool Equals(object? obj) =>
            obj is EnableAugmentedEnumerationExtensionTargetData eatd && eatd == this;
        public bool Equals(EnableAugmentedEnumerationExtensionTargetData other) => other == this;

        /// <inheritdoc />
        public override string ToString() =>
            $"{nameof(EnableAugmentedEnumerationExtensionTargetData)} -- {nameof(ClassToAugment)}: {ClassToAugment.Identifier.Text}; " +
            $"{nameof(AttributeSyntax)}: {AttributeSyntax.Name}; {nameof(AttributeTargetDataSyntax)}: " +
            $"{AttributeTargetDataSyntax.Type}.";


        public readonly struct EqComp : IByRoRefEqualityComparer<EnableAugmentedEnumerationExtensionTargetData>
        {

            public static implicit operator ByRoRefEqTest<EnableAugmentedEnumerationExtensionTargetData>(EqComp _) =>
                TheEqualityTest;

            public static implicit operator ByRoRefHasher<EnableAugmentedEnumerationExtensionTargetData>(EqComp _) =>
                TheHasher;

            /// <inheritdoc />
            [Pure]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(in EnableAugmentedEnumerationExtensionTargetData lhs,
                in EnableAugmentedEnumerationExtensionTargetData rhs)
                => lhs == rhs;

            /// <inheritdoc />
            public int GetHashCode(in EnableAugmentedEnumerationExtensionTargetData val)
                => val.GetHashCode();

            static EqComp()
            {
                var comp = default(EqComp);
                TheEqualityTest =
                (in EnableAugmentedEnumerationExtensionTargetData lhs,
                    in EnableAugmentedEnumerationExtensionTargetData rhs) => comp.Equals(in lhs, in rhs);
                TheHasher = (in EnableAugmentedEnumerationExtensionTargetData obj) => comp.GetHashCode(in obj);

            }

            private static readonly ByRoRefEqTest<EnableAugmentedEnumerationExtensionTargetData> TheEqualityTest;
            private static readonly ByRoRefHasher<EnableAugmentedEnumerationExtensionTargetData> TheHasher;
        }

        private readonly EnableFastLinkExtensionsTargetData _base;
        private static readonly EqualityComparer<TypeOfExpressionSyntax> TheTpsComparer = EqualityComparer<TypeOfExpressionSyntax>.Default;
    }
}