using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cjm.CodeGen
{
    public readonly struct EnableAugmentedEnumerationExtensionTargetData : ITargetData,
        IEquatable<EnableAugmentedEnumerationExtensionTargetData>
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
        public override bool Equals(object? obj) =>
            obj is EnableAugmentedEnumerationExtensionTargetData eatd && eatd == this;
        public bool Equals(EnableAugmentedEnumerationExtensionTargetData other) => other == this;

        /// <inheritdoc />
        public override string ToString() =>
            $"{nameof(EnableAugmentedEnumerationExtensionTargetData)} -- {nameof(ClassToAugment)}: {ClassToAugment.Identifier.Text}; " +
            $"{nameof(AttributeSyntax)}: {AttributeSyntax.Name}; {nameof(AttributeTargetDataSyntax)}: " +
            $"{AttributeTargetDataSyntax.Type}.";
        

        private readonly EnableFastLinkExtensionsTargetData _base;
        private static readonly EqualityComparer<TypeOfExpressionSyntax> TheTpsComparer = EqualityComparer<TypeOfExpressionSyntax>.Default;
    }
}