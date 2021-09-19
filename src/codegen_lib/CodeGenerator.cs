using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LoggerLibrary;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cjm.CodeGen
{
    public readonly struct StructIEnumeratorByTValGenerator : IEquatable<StructIEnumeratorByTValGenerator>
    {
        public static StructIEnumeratorByTValGenerator CreateGenerator(string template, string generatorName, ClassDeclarationSyntax cdsToAugment, UsableSemanticData semanticData)
        {
            if (generatorName == null) throw new ArgumentNullException(nameof(generatorName));
            if (cdsToAugment == null) throw new ArgumentNullException(nameof(cdsToAugment));
            if (semanticData == null) throw new ArgumentNullException(nameof(semanticData));
            if (string.IsNullOrWhiteSpace(generatorName))
                throw new ArgumentException(@"Expected a string with some non-whitespace characters.",
                    nameof(generatorName));
            const int numExpectedFormatParams = 13;
            ValidateProperNumberOfFormatArgs(template, numExpectedFormatParams, nameof(template));

            string targetCollectionTypeNameNoNamespace =
                semanticData.GenerationInfo.TargetCollectionType.ToDisplayString();
            int lastDot = targetCollectionTypeNameNoNamespace.LastIndexOf(semanticData.GenerationInfo.TargetCollectionType.Name, StringComparison.InvariantCulture);
            if (lastDot > -1)
                targetCollectionTypeNameNoNamespace = targetCollectionTypeNameNoNamespace.Substring(lastDot);

            
            (string Key, string Value)[] kvps = {
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
                new("{12}", string.Empty),

            };
            const string genericCollectionsNamespace = "System.Collections.Generic";
            string wrappedCollectionNamespace =
                semanticData.GenerationInfo.TargetCollectionType.ContainingNamespace.ToDisplayString();
            kvps[0].Value =
                !TrimmedStringComparer.TrimmedOrdinal.Equals(genericCollectionsNamespace, wrappedCollectionNamespace)
                    ? $"using {wrappedCollectionNamespace};"
                    : string.Empty;
            kvps[1].Value=  semanticData.GenerationInfo.StaticClassToAugment.ContainingNamespace.ToDisplayString();
            kvps[2].Value = semanticData.GenerationInfo.StaticClassToAugment.Name;
            kvps[3].Value = "public"; //for now
            kvps[4].Value = $"Wrapped{semanticData.GenerationInfo.TargetCollectionType.Name}Of{semanticData.GenerationInfo.TargetItemType.Name}ByVal"; //for now
            kvps[5].Value = targetCollectionTypeNameNoNamespace;
            string ctorFormatStr = semanticData.GenerationInfo.TargetCollectionType.IsReferenceType
                ? ReferenceTypeCtorFrmtStr
                : ValueTypeCtorFrmtStr;
            kvps[6].Value = string.Format(ctorFormatStr, kvps[4].Value, kvps[5].Value);
            kvps[7].Value = semanticData.GenerationInfo.TargetCollectionType.IsReferenceType ? "readonly" : string.Empty;
            kvps[8].Value = semanticData.GenerationInfo.TargetItemType.ToDisplayString();
            kvps[9].Value = semanticData.GenerationInfo.EnumeratorType.ToDisplayString();
            kvps[11].Value = semanticData.GenerationInfo.EnumeratorData.HasProperPublicDispose
                ? PublicDisposeMethod
                : NonPublicDispose;
            kvps[10].Value = semanticData.GenerationInfo.EnumeratorData.HasProperPublicReset
                ? PublicResetMethod
                : NonPublicReset;
            kvps[12].Value =
                semanticData.GenerationInfo.EnumeratorData.HasProperPublicDispose &&
                semanticData.GenerationInfo.EnumeratorData.ImplementsIDisposable
                    ? ImplementingIDisposable
                    : NotImplementingIDisposable;


            Debug.Assert(kvps.All(itm => itm.Key != null && itm.Value != null));
            var immut = kvps.ToImmutableSortedDictionary(kvp => kvp.Key, kvp => kvp.Value,
                TrimmedStringComparer.TrimmedOrdinalIgnoreCase);
            if (immut.Count != kvps.Length)
            {
                throw new ArgumentException(
                    $"One or more duplicate keys found in lookup for {nameof(StructIEnumeratorByTValGenerator)}.");
            }


            string constructedGeneratorName =
                $"{semanticData.GenerationInfo.StaticClassToAugment.ContainingAssembly.Name}_{semanticData.GenerationInfo.StaticClassToAugment.Name}_{kvps[5].Value}";
            constructedGeneratorName = constructedGeneratorName.Replace("<", "Of").Replace(">", "");
            
            ValidateCharacters(constructedGeneratorName.AsSpan());

            return new StructIEnumeratorByTValGenerator(constructedGeneratorName, template, immut);

            [Conditional("DEBUG")]
            static void ValidateCharacters(ReadOnlySpan<char> span)
            {
                char first = span[0];
                if (!SyntaxFacts.IsIdentifierStartCharacter(first))
                    throw new ArgumentException(
                        @$"Starting character {first} is not a valid starting character for an identifier.",
                        nameof(constructedGeneratorName));
                ReadOnlySpan<char> permitted = stackalloc char[]
                {
                    '.',
                    ',',
                    '-',
                    '_',
                    ' ',
                    '(',
                    ')',
                    '[',
                    ']',
                    '{',
                    '}'
                };
                for (int i = 1; i < span.Length; ++i)
                {
                    if (!SyntaxFacts.IsIdentifierPartCharacter(span[i]) && !Contains(permitted, span[i]))
                    {
                        throw new ArgumentException(@$"In {span.ToString()}, the character {span[i]} is not permitted.",
                            nameof(constructedGeneratorName));
                    }
                }

            }

            static bool Contains(in ReadOnlySpan<char> permittedChars, char findMeInPermitted)
            {
                foreach (char c in permittedChars)
                {
                    if (c == findMeInPermitted)
                        return true;
                }

                return false;
            }
        }

        public static readonly StructIEnumeratorByTValGenerator InvalidDefault = default;
        public bool IsInvalidDefault => !_initialized;
        public string Template => IsInvalidDefault ? string.Empty : _template;
        public IEnumerable<string> Items => _parameters.Values;
        public IEnumerable<string> ReplacementKeys => _parameters.Keys;
        public string GeneratorHintName => _generatorName;

        private StructIEnumeratorByTValGenerator(string generatorName, string template, ImmutableSortedDictionary<string, string> items)
        {
            _generatorName = (generatorName) switch
            {
                null => throw new ArgumentNullException(nameof(generatorName)),
                { } txt when string.IsNullOrWhiteSpace(generatorName) => throw new ArgumentException(
                    @"Expected a string with some non-whitespace characters.",
                    nameof(generatorName)),
                { } txt => txt.Trim(),
            };
            
            _parameters = items switch
            {
                null => throw new ArgumentNullException(nameof(items)),
                { } when items.Any(itm => itm.Key == null || itm.Value == null) => throw new ArgumentException("One or more items was null."),
                _ => items
            };
            
            _template = template ?? throw new ArgumentNullException(nameof(template));
            _regex = new Regex(string.Join("|", items.Keys.Select(Regex.Escape)));
            _initialized = true;
        }

       
        // ReSharper disable once CoVariantArrayConversion -- will not write
        public (string Name, string GeneratedCode) Generate()
        {
            if (IsInvalidDefault) throw new InvalidOperationException("The generator is not initialized.");
            string code = PerformReplacement();
            return (_generatorName, code);
        }

        public override int GetHashCode()
        {
            if (IsInvalidDefault)
                return int.MinValue;

            int hash = _template.GetHashCode();
            unchecked
            {
                hash = (hash * 397) ^ TrimmedStringComparer.TrimmedOrdinalIgnoreCase.GetHashCode(_generatorName);
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
            lhs._template == rhs._template && TrimmedStringComparer.TrimmedOrdinal.Equals(lhs._generatorName, rhs._generatorName) && (ReferenceEquals(lhs._parameters, rhs._parameters) ||
                                               lhs._parameters.SequenceEqual(rhs._parameters));

        public static bool operator
            !=(in StructIEnumeratorByTValGenerator lhs, in StructIEnumeratorByTValGenerator rhs) => !(lhs == rhs);

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is StructIEnumeratorByTValGenerator gen && gen == this;

        public bool Equals(StructIEnumeratorByTValGenerator other) => other == this;

        /// <inheritdoc />
        public override string ToString() => IsInvalidDefault
            ? "INVALID DEFAULT"
            : $"{nameof(StructIEnumeratorByTValGenerator)} for \"{GeneratorHintName}\": {Environment.NewLine}\tTemplate: \t{Template};{Environment.NewLine}" +
              $" \tArgCount: \t{_parameters.Count}.";

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

        private string PerformReplacement()
        {
            var dict = _parameters;
            return _regex.Replace(_template, m => dict[m.Value]);
        }

        private readonly ImmutableSortedDictionary<string, string> _parameters;
        private readonly Regex _regex;
        private readonly string _template;
        private readonly string _generatorName;
        private readonly bool _initialized;

        private const string PublicDisposeMethod = @"[MethodImpl(MethodImplOptions.AggressiveInlining)] public void Dispose() => _wrapped.Dispose();";
        private const string NonPublicDispose = "";//@"void IEnumerable.Dispose() => ((IEnumerable) _wrapped).Dispose();";
        private const string PublicResetMethod = @"[MethodImpl(MethodImplOptions.AggressiveInlining)] public void Reset() => _wrapped.Reset()";
        private const string NonPublicReset = "";// @"void IEnumerable.Reset() => ((IEnumerable) _wrapped).Reset();";
        private const string ImplementingIDisposable = ": IDisposable";
        private const string NotImplementingIDisposable = "";


        private const string ReferenceTypeCtorFrmtStr =
            @"private {0}({1} col) => _wrapped = col ?? throw new ArgumentNullException(nameof(col));";

        private const string ValueTypeCtorFrmtStr =
            @"private {0}({1} col) => _wrapped = col;";


    }
}
