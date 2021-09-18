using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LoggerLibrary;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cjm.CodeGen
{
    public readonly struct StructIEnumeratorByTValGenerator : IEquatable<StructIEnumeratorByTValGenerator>
    {
        public static StructIEnumeratorByTValGenerator CreateGenerator(string template, ClassDeclarationSyntax cdsToAugment, UsableSemanticData semanticData)
        {
            if (cdsToAugment == null) throw new ArgumentNullException(nameof(cdsToAugment));
            if (semanticData == null) throw new ArgumentNullException(nameof(semanticData));
            const int numExpectedFormatParams = 12;
            ValidateProperNumberOfFormatArgs(template, numExpectedFormatParams, nameof(template));

            string targetCollectionTypeNameNoNamespace =
                semanticData.GenerationInfo.TargetCollectionType.ToDisplayString();
            int lastDot = targetCollectionTypeNameNoNamespace.LastIndexOf(semanticData.GenerationInfo.TargetCollectionType.Name, StringComparison.InvariantCulture);
            if (lastDot > -1)
                targetCollectionTypeNameNoNamespace = targetCollectionTypeNameNoNamespace.Substring(lastDot);

            (string Key, string Value)[] kvps = 
            {
                new("{0}", string.Empty ),
                new("{1}", string.Empty ),
                new("{2}", string.Empty ),
                new("{3}", string.Empty ),
                new("{4}", string.Empty ),

                new("{5}", string.Empty ),
                new("{6}", string.Empty ),
                new("{7}", string.Empty ),
                new("{8}", string.Empty ),
                new("{9}", string.Empty ),

                new("{10}", string.Empty ),
                new("{11}", string.Empty ),

            };
            
            string wrappedCollectionNamespace =
                semanticData.GenerationInfo.TargetCollectionType.ContainingNamespace.ToDisplayString();
            kvps[0].Value = $"using {wrappedCollectionNamespace};";
            kvps[1].Value=  semanticData.GenerationInfo.StaticClassToAugment.ContainingNamespace.ToDisplayString();
            kvps[2].Value = semanticData.GenerationInfo.StaticClassToAugment.Name;
            kvps[3].Value = "public"; //for now
            kvps[4].Value = $"WrappedListOf{semanticData.GenerationInfo.TargetItemType.Name}ByVal"; //for now
            kvps[5].Value = targetCollectionTypeNameNoNamespace;
            string ctorFormatStr = semanticData.GenerationInfo.TargetCollectionType.IsReferenceType
                ? ReferenceTypeCtorFrmtStr
                : ValueTypeCtorFrmtStr;
            kvps[6].Value = string.Format(ctorFormatStr, kvps[4], kvps[5]);
            kvps[7].Value = semanticData.GenerationInfo.TargetCollectionType.IsReferenceType ? "readonly" : string.Empty;
            kvps[8].Value = semanticData.GenerationInfo.TargetItemType.ToDisplayString();
            kvps[9].Value = semanticData.GenerationInfo.EnumeratorType.ToDisplayString();
            kvps[11].Value = semanticData.GenerationInfo.EnumeratorData.HasProperPublicDispose
                ? PublicDisposeMethod
                : NonPublicDispose;
            kvps[10].Value = semanticData.GenerationInfo.EnumeratorData.HasProperPublicReset
                ? PublicResetMethod
                : NonPublicReset;
            Debug.Assert(kvps.All(itm => itm.Key != null && itm.Value != null));
            var immut = kvps.ToImmutableSortedDictionary(kvp => kvp.Key, kvp => kvp.Value,
                TrimmedStringComparer.TrimmedOrdinalIgnoreCase);
            if (immut.Count != kvps.Length)
            {
                throw new ArgumentException(
                    $"One or more duplicate keys found in lookup for {nameof(StructIEnumeratorByTValGenerator)}.");
            }
            return new StructIEnumeratorByTValGenerator(template, immut);
        }

        public static readonly StructIEnumeratorByTValGenerator InvalidDefault = default;
        public bool IsInvalidDefault => !_initialized;
        public string Template => IsInvalidDefault ? string.Empty : _template;

        public IEnumerable<string> Items
        {
            get
            {
                if (IsInvalidDefault)
                    yield break;
                foreach (string t in _parameters.Values)
                {
                    yield return t;
                }
            }
        }

        private StructIEnumeratorByTValGenerator(string template, ImmutableSortedDictionary<string, string> items)
        {
            _parameters = items switch
            {
                null => throw new ArgumentNullException(nameof(items)),
                { } when items.Any(itm => itm.Key == null || itm.Value == null) => throw new ArgumentException("One or more items was null."),
                _ => items
            };
            
            _template = template ?? throw new ArgumentNullException(nameof(template));
            _initialized = true;
        }

        // ReSharper disable once CoVariantArrayConversion -- will not write
        public string Generate() => PerformReplacement();

        private string PerformReplacement()
        {
            throw new NotImplementedException();
        }

        public override int GetHashCode()
        {
            if (IsInvalidDefault)
                return int.MinValue;

            int hash = _template.GetHashCode();
            unchecked
            {
                hash = (hash * 397) ^ _parameters.Count;
                foreach (var kvp in _parameters)
                {
                    hash = (hash * 397) ^ kvp.Key.GetHashCode();
                    hash = (hash * 397) ^ kvp.Value.GetHashCode();
                }
            }
            return hash;
        }

        public static bool operator
            ==(in StructIEnumeratorByTValGenerator lhs, in StructIEnumeratorByTValGenerator rhs) => lhs.IsInvalidDefault == rhs.IsInvalidDefault &&
            !lhs.IsInvalidDefault &&
            lhs._template == rhs._template && (ReferenceEquals(lhs._parameters, rhs._parameters) ||
                                               lhs._parameters.SequenceEqual(rhs._parameters));

        public static bool operator
            !=(in StructIEnumeratorByTValGenerator lhs, in StructIEnumeratorByTValGenerator rhs) => !(lhs == rhs);

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is StructIEnumeratorByTValGenerator gen && gen == this;

        public bool Equals(StructIEnumeratorByTValGenerator other) => other == this;

        /// <inheritdoc />
        public override string ToString() => IsInvalidDefault
            ? "INVALID DEFAULT"
            : $"{nameof(StructIEnumeratorByTValGenerator)} -- Template: {_template};{Environment.NewLine} " +
              $"ArgCount: {_parameters.Count}.";

        private static void ValidateProperNumberOfFormatArgs(string template, int numberFormatArgs, string templateParamName)
        {
            if (numberFormatArgs < 0)
                throw new ArgumentOutOfRangeException(nameof(numberFormatArgs), numberFormatArgs,
                    @"Expected non-negative.");

            ImmutableHashSet<string> expected = ExpectedFormatTokens(numberFormatArgs).ToImmutableHashSet();
            Debug.Assert(expected.Count == numberFormatArgs);
            var res = Regex.Matches(template, @"\{\d+[^\{\}]*\}");
            ImmutableHashSet<string> actual = (from Match item in res
                                               where item.Success && !string.IsNullOrWhiteSpace(item.Value)
                                               select item.Value).ToImmutableHashSet();

            if (actual.SetEquals(expected))
            {
                return;
            }

            var actualButNotExpected = actual.Except(expected);
            var expectedButNotActual = expected.Except(actual);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("The collection does not have the correct format arguments.");
            foreach (string item in actualButNotExpected)
            {
                sb.AppendLine($" \t\tArgument {item} appeared but was not expected.");
            }
            foreach (string item in expectedButNotActual)
            {
                sb.AppendLine($" \t\tArgument {item} was expected but did not appear.");
            }

            throw new ArgumentException(sb.ToString(), templateParamName);

            static IEnumerable<string> ExpectedFormatTokens(int expected)
            {
                while (--expected > -1)
                {
                    yield return "{" + expected + "}";
                }
            }
        }

        private readonly ImmutableSortedDictionary<string, string> _parameters;
        private readonly string _template;
        private readonly bool _initialized;

        private const string PublicDisposeMethod = @"public void Dispose() => _wrapped.Dispose();";
        private const string NonPublicDispose = @"void IEnumerable.Dispose() => ((IEnumerable) _wrapped).Dispose();";
        private const string PublicResetMethod = @"public void Reset() => _wrapped.Reset()";
        private const string NonPublicReset = @"void IEnumerable.Reset() => ((IEnumerable) _wrapped).Reset();";

        private const string ReferenceTypeCtorFrmtStr =
            @"private {0}({1} col) => _wrapped = col ?? throw new ArgumentNullException(nameof(col));";

        private const string ValueTypeCtorFrmtStr =
            @"private {0}({1} col) => _wrapped = col;";


    }
}
