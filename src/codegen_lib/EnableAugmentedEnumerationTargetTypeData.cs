using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Cjm.CodeGen
{
    public readonly struct
        EnableAugmentedEnumerationTargetTypeData : IEquatable<EnableAugmentedEnumerationTargetTypeData>, 
            IHasGenericByRefRoEqComparer<EnableAugmentedEnumerationTargetTypeData.EqComp, EnableAugmentedEnumerationTargetTypeData>
    {
        public static EnableAugmentedEnumerationTargetTypeData CreateFailureDoesNotSpecifyANamedTypeTargetTypeDataNoTsAvailable(
            AttributeArgumentSyntax firstAttribArg, Location attributeLocation, TypeOfExpressionSyntax toes,
            in TypeInfo ti, TypeKind tk)
        {
            if (firstAttribArg == null) throw new ArgumentNullException(nameof(firstAttribArg));
            if (attributeLocation == null) throw new ArgumentNullException(nameof(attributeLocation));
            if (toes == null) throw new ArgumentNullException(nameof(toes));
            
            string reasonForInvalidity =
                $"A type symbol for the type specified by the typeof expression could not be identified..";
            Location l = toes.GetLocation();
            TextSpan ts = toes.Span;
            return new EnableAugmentedEnumerationTargetTypeData(firstAttribArg, l, in ts, toes, reasonForInvalidity,
                in ti, tk, null);
        }

        public static EnableAugmentedEnumerationTargetTypeData CreateFailureDoesNotSpecifyANamedTypeTargetTypeData(
            AttributeArgumentSyntax firstAttribArg, Location attributeLocation, TypeOfExpressionSyntax toes,
            in TypeInfo ti, TypeKind tk, ITypeSymbol badTs)
        {
            if (firstAttribArg == null) throw new ArgumentNullException(nameof(firstAttribArg));
            if (attributeLocation == null) throw new ArgumentNullException(nameof(attributeLocation));
            if (toes == null) throw new ArgumentNullException(nameof(toes));
            if (badTs == null) throw new ArgumentNullException(nameof(badTs));

            string reasonForInvalidity =
                $"The type specified by typeof expression is not a named type symbol.  It is of type \"{badTs.GetType().Name}\" and has value: {badTs}.";
            Location l = toes.GetLocation();
            TextSpan ts = toes.Span;
            return new EnableAugmentedEnumerationTargetTypeData(firstAttribArg, l, in ts, toes, reasonForInvalidity,
                in ti, tk, null);
        }
        public static EnableAugmentedEnumerationTargetTypeData CreateFailureAttributeLacksArgumentList(
            AttributeSyntax attribSyntax)
        {
            if (attribSyntax == null) throw new ArgumentNullException(nameof(attribSyntax));

            const string badnessReason = "The attribute syntax lacks an argument list.";
            Location l = attribSyntax.GetLocation();
            TextSpan attribSpan = attribSyntax.Span;
            return new EnableAugmentedEnumerationTargetTypeData(null, l, in attribSpan, null, badnessReason, default,
                TypeKind.Unknown, null);
        }

        public static EnableAugmentedEnumerationTargetTypeData CreateFailureFirstTypeArgIsNotTypeofExpressionSyntax(
            AttributeArgumentSyntax attribSyntax, ExpressionSyntax expr)
        {
            const string badnessReason = "The attribute's first argument is not a valid typeof expression.";
            Location l = expr.GetLocation();
            TextSpan s = expr.Span;
            return new EnableAugmentedEnumerationTargetTypeData(attribSyntax, l, in s, null, badnessReason, default,
                TypeKind.Unknown, null);
        }

        public static EnableAugmentedEnumerationTargetTypeData CreateSuccessTargetTypeData(
            AttributeArgumentSyntax firstAttribArg, Location attributeLocation, TypeOfExpressionSyntax toes,
            in TypeInfo ti, TypeKind tk, INamedTypeSymbol targetNts)
        {
            if (firstAttribArg == null) throw new ArgumentNullException(nameof(firstAttribArg));
            if (attributeLocation == null) throw new ArgumentNullException(nameof(attributeLocation));
            if (toes == null) throw new ArgumentNullException(nameof(toes));
            if (targetNts == null) throw new ArgumentNullException(nameof(targetNts));

            string reasonForInvalidity = string.Empty;
            Location l = toes.GetLocation();
            TextSpan ts = toes.Span;
            return new EnableAugmentedEnumerationTargetTypeData(firstAttribArg, l, in ts, toes, reasonForInvalidity,
                in ti, tk, targetNts);

        }

        public bool IsGoodMatch => FirstArgument != null && TypeOfSyntax != null && TargetTypeSymbol != null;

        public AttributeArgumentSyntax? FirstArgument => _firstArgument;
        public Location AttributeOrErrorLocation => _attributeOrErrorLocation ?? Location.None;
        public TypeOfExpressionSyntax? TypeOfSyntax => _typeOfSyntax;
        public TextSpan TargetTypeOrErrorTextSpan => _errorOrTargetTypeExpressionSpan;
        public string ReasonForInvalidity => _reasonForInvalidity ?? DefaultBadReason;
        public TypeInfo TargetTypeInformation => _typeInfo;
        public TypeKind TargetTypeKind => _typeKind;
        public INamedTypeSymbol? TargetTypeSymbol => _targetNts;

        /// <inheritdoc />
        public override int GetHashCode()
        {
            int hash = _typeKind.GetHashCode();
            unchecked
            {
                hash = (hash * 397) ^ (_targetNts == null
                    ? int.MinValue
                    : SymbolEqualityComparer.Default.GetHashCode(_targetNts));
                hash = (hash * 397) ^ AttributeOrErrorLocation.GetHashCode();
                hash = (hash * 397) ^ _errorOrTargetTypeExpressionSpan.GetHashCode();
            }
            return hash;
        }

        /// <inheritdoc />
        public EqComp GetComparer() => default;

        public override bool Equals(object? other) =>
            other is EnableAugmentedEnumerationTargetTypeData ttd && ttd == this;

        public bool Equals(EnableAugmentedEnumerationTargetTypeData other) => other == this;

        public static bool operator ==(in EnableAugmentedEnumerationTargetTypeData lhs,
            in EnableAugmentedEnumerationTargetTypeData rhs)
        {
            return AreEqual(lhs._firstArgument, rhs._firstArgument) && AreEqual(lhs._typeOfSyntax, rhs._typeOfSyntax) &&
                   AreEqual(lhs.AttributeOrErrorLocation, rhs.AttributeOrErrorLocation) &&
                   lhs._errorOrTargetTypeExpressionSpan == rhs._errorOrTargetTypeExpressionSpan &&
                   SymbolEqualityComparer.Default.Equals(lhs._targetNts, rhs._targetNts) &&
                   StringComparer.Ordinal.Equals(lhs.ReasonForInvalidity, rhs.ReasonForInvalidity) &&
                   lhs._typeInfo.Equals(rhs._typeInfo) && lhs._typeKind == rhs._typeKind;

            static bool AreEqual<T>(T? l, T? r) where T : class => ReferenceEquals(l, r) || l?.Equals(r) == true;
        }

        public static bool operator !=(in EnableAugmentedEnumerationTargetTypeData lhs,
            in EnableAugmentedEnumerationTargetTypeData rhs) => !(lhs == rhs);

        /// <inheritdoc />
        public override string ToString()
            //IsGoodMatch implies accessed nullable attributes are non-null
            => IsGoodMatch
                ? $"Typeof expression: {TypeOfSyntax!} yields a target {nameof(TypeKind)} of {TargetTypeKind} and a type symbol with name \"{_targetNts!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}\"."
                : $"The attribute does not have a valid target type.  Reason: \"{ReasonForInvalidity}\".";


        private EnableAugmentedEnumerationTargetTypeData(AttributeArgumentSyntax? aas, Location? place, in TextSpan ts,
            TypeOfExpressionSyntax? toes, string? reasonForInvalidity, in TypeInfo ti, TypeKind tk, INamedTypeSymbol? nts)
        {
            _firstArgument = aas;
            _attributeOrErrorLocation = place ?? Location.None;
            _errorOrTargetTypeExpressionSpan = ts;
            _typeOfSyntax = toes;
            _reasonForInvalidity = reasonForInvalidity ?? string.Empty;
            _typeInfo = ti;
            _typeKind = tk;
            _targetNts = nts;
        }

        private readonly AttributeArgumentSyntax? _firstArgument;
        private readonly Location? _attributeOrErrorLocation;
        private readonly TextSpan _errorOrTargetTypeExpressionSpan;
        private readonly TypeOfExpressionSyntax? _typeOfSyntax;
        private readonly string? _reasonForInvalidity;
        private readonly TypeInfo _typeInfo;
        private readonly TypeKind _typeKind;
        private readonly INamedTypeSymbol? _targetNts;

         
        private const string DefaultBadReason = "The " + nameof(EnableAugmentedEnumerationTargetTypeData) +
                                                " struct is not properly initialized.";

        public readonly struct EqComp : IByRoRefEqualityComparer<EnableAugmentedEnumerationTargetTypeData>
        {
            public bool Equals(in EnableAugmentedEnumerationTargetTypeData l,
                in EnableAugmentedEnumerationTargetTypeData r) => l == r;

            public int GetHashCode(in EnableAugmentedEnumerationTargetTypeData o) => o.GetHashCode();
        }


        public static EnableAugmentedEnumerationTargetTypeData CreateFailureBadTypeKind(AttributeArgumentSyntax firstParam, Location location, TypeOfExpressionSyntax toes, in TypeInfo ti, string reasonWhyNot, TypeKind tk, ITypeSymbol? badTs)
        {
            if (firstParam == null) throw new ArgumentNullException(nameof(firstParam));
            if (location == null) throw new ArgumentNullException(nameof(location));
            if (toes == null) throw new ArgumentNullException(nameof(toes));
            if (reasonWhyNot == null) throw new ArgumentNullException(nameof(reasonWhyNot));
            if (string.IsNullOrWhiteSpace(reasonWhyNot))
            {
                reasonWhyNot = "No reason available.";
            }

            if (badTs != null)
            {
                reasonWhyNot +=
                    $" Type symbol was of type {badTs.GetType().Name} and string rep is \"{badTs.ToDisplayString()}\".";
            }

            return new EnableAugmentedEnumerationTargetTypeData(firstParam, location, toes.Span, toes, reasonWhyNot,
                in ti, tk, null);
        }
    }
}