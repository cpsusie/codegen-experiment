using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cjm.CodeGen
{
    public readonly struct EnableFastLinkExtensionsTargetData : ITargetData,
        IEquatable<EnableFastLinkExtensionsTargetData>
    {
        public static EnableFastLinkExtensionsTargetData
            CreateFastLinkExtensionsTargetData(ClassDeclarationSyntax cds, AttributeSyntax ats) =>
            new EnableFastLinkExtensionsTargetData(cds ?? throw new ArgumentNullException(nameof(cds)),
                ats ?? throw new ArgumentNullException(nameof(ats)));

        public ClassDeclarationSyntax ClassToAugment { get; }
        public AttributeSyntax AttributeSyntax { get; }
        
        private EnableFastLinkExtensionsTargetData(ClassDeclarationSyntax cds, AttributeSyntax ats)
        {
            ClassToAugment = cds;
            AttributeSyntax = ats;
        }

        public static bool operator
            ==(EnableFastLinkExtensionsTargetData lhs, EnableFastLinkExtensionsTargetData rhs) =>
            TheCdsComp.Equals(lhs.ClassToAugment, rhs.ClassToAugment) &&
            TheAttComp.Equals(lhs.AttributeSyntax, rhs.AttributeSyntax);

        public static bool operator
            !=(EnableFastLinkExtensionsTargetData lhs, EnableFastLinkExtensionsTargetData rhs) => !(lhs == rhs);

        public override int GetHashCode()
        {
            int hash = TheCdsComp.GetHashCode(ClassToAugment);
            unchecked
            {
                hash = (hash * 397) ^ TheAttComp.GetHashCode(AttributeSyntax);
            }

            return hash;
        }
        public override bool Equals(object other) => other is EnableFastLinkExtensionsTargetData efetg && efetg == this;

        public bool Equals(EnableFastLinkExtensionsTargetData other) => other == this;

        /// <inheritdoc />
        public override string ToString() =>
            $"{nameof(EnableFastLinkExtensionsTargetData)} -- {nameof(ClassToAugment)}: {ClassToAugment.Identifier.Text}; {nameof(AttributeSyntax)}: {AttributeSyntax.Name}.";
        

        private static readonly EqualityComparer<AttributeSyntax> TheAttComp = EqualityComparer<AttributeSyntax>.Default;
        private static readonly EqualityComparer<ClassDeclarationSyntax> TheCdsComp = EqualityComparer<ClassDeclarationSyntax>.Default;
    }
}