using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HpTimeStamps;
using JetBrains.Annotations;

namespace Cjm.Templates.Test
{
    public readonly record struct TemplateInterfaceExpectedResults(TemplateInterfaceTestCaseIdentifier Identifier, int ExpectedNumberHits, string TestName, string Code, ImmutableHashSet<string> FoundNames);

    public sealed class TemplateInterfaceDetectionFixture
    {
        public ImmutableSortedDictionary<TemplateInterfaceTestCaseIdentifier, TemplateInterfaceExpectedResults>
            Lookup => TheLookup;

        static TemplateInterfaceDetectionFixture()
        {
            TheComparer = TrimmedStringComparer.TrimmedOrdinalIgnoreCase;
            TheLookup = EnumerateResults()
                .Select(itm =>
                    new KeyValuePair<TemplateInterfaceTestCaseIdentifier, TemplateInterfaceExpectedResults>(
                        itm.Identifier, itm)).ToImmutableSortedDictionary();
            if (!AdditionalTemplatesRepository.AreTemplatesFixedNow)
            {
                AdditionalTemplatesRepository.SupplyAlternateTemplatesOrThrow(ExtraUncompilableTemplateImplementations());
            }
        }

        public static IEnumerable<string> ExtraUncompilableTemplateImplementations()
        {
            yield return TestCasesTemplateInterface.EnumComparerUncompilableImpl;
        }

        private static IEnumerable<TemplateInterfaceExpectedResults> EnumerateResults()
        {
            yield return new TemplateInterfaceExpectedResults(TemplateInterfaceTestCaseIdentifier.NoHitCase, 0,
                TemplateInterfaceTestCaseIdentifier.NoHitCase.ToString(), TestCasesTemplateInterface.IEnumEqualityComparer, ImmutableHashSet<string>.Empty);
            yield return new TemplateInterfaceExpectedResults(TemplateInterfaceTestCaseIdentifier.EnumComparer, 1,
                TemplateInterfaceTestCaseIdentifier.EnumComparer.ToString(), TestCasesTemplateInterface.IEnumComparer,
                new[] { "IEnumComparer" }.CreateHashSet(TheComparer));
            yield return new TemplateInterfaceExpectedResults(TemplateInterfaceTestCaseIdentifier.EnumComparerImpl,3,
                TemplateInterfaceTestCaseIdentifier.EnumComparerImpl.ToString(),
                 TestCasesTemplateInterface.IEnumComparer, new[] { "TotalOrderProviderImpl", "EnumComparerUncompilableImpl" }.CreateHashSet(TheComparer));
            yield return new TemplateInterfaceExpectedResults(TemplateInterfaceTestCaseIdentifier.EnumComparerInstant, 4,
                TemplateInterfaceTestCaseIdentifier.EnumComparerInstant.ToString(),
                TestCasesTemplateInterface.EnumComparerWithInstantiation, new[] { "ForfKlorComparer" }.CreateHashSet(TheComparer));

        }

        private static readonly ImmutableSortedDictionary<TemplateInterfaceTestCaseIdentifier, TemplateInterfaceExpectedResults> TheLookup;
        private static readonly TrimmedStringComparer TheComparer;

    }

    public static class StringKeySetDicExtensions
    {
        public static ImmutableSortedDictionary<string, TValue>.Builder
            CreateSortedDictionaryBuilder<TValue>(this TrimmedStringComparer comparer) =>
            ImmutableSortedDictionary.CreateBuilder<string, TValue>(comparer ??
                                                                    throw new ArgumentNullException(nameof(comparer)));

        public static ImmutableDictionary<string, TValue>.Builder
            CreateDictionaryBuilder<TValue>(this TrimmedStringComparer comparer) =>
            ImmutableDictionary.CreateBuilder<string, TValue>(comparer ??
                                                                    throw new ArgumentNullException(nameof(comparer)));

        public static ImmutableSortedSet<string>.Builder CreateSortedSetBuilder(this TrimmedStringComparer comparer) =>
            ImmutableSortedSet.CreateBuilder<string>(comparer ?? throw new ArgumentNullException(nameof(comparer)));

        public static ImmutableHashSet<string>.Builder CreateHashSetBuilder(this TrimmedStringComparer comparer) =>
            ImmutableHashSet.CreateBuilder<string>(comparer ?? throw new ArgumentNullException(nameof(comparer)));

        public static ImmutableSortedDictionary<string, TValue>
            CreateSortedDictionary<TValue>(this IEnumerable<KeyValuePair<string, TValue>> items,
                TrimmedStringComparer comparer) =>
            EnumerateThrowIfNull(items ?? throw new ArgumentNullException(nameof(items)))
                .ToImmutableSortedDictionary(comparer ?? throw new ArgumentNullException(nameof(comparer)));

        public static ImmutableDictionary<string, TValue>
            CreateDictionary<TValue>(this IEnumerable<KeyValuePair<string, TValue>> items,
                TrimmedStringComparer comparer) =>
            EnumerateThrowIfNull(items ?? throw new ArgumentNullException(nameof(items)))
                .ToImmutableDictionary(comparer ?? throw new ArgumentNullException(nameof(comparer)));

        public static ImmutableSortedSet<string> CreateSortedSet(this IEnumerable<string> items,
            TrimmedStringComparer comparer) =>
            EnumerateThrowIfNull(items ?? throw new ArgumentNullException(nameof(items)))
                .ToImmutableSortedSet(comparer ?? throw new ArgumentNullException(nameof(comparer)));

        public static ImmutableHashSet<string> CreateHashSet(this IEnumerable<string> items,
            TrimmedStringComparer comparer) =>
            EnumerateThrowIfNull(items ?? throw new ArgumentNullException(nameof(items)))
                .ToImmutableHashSet(comparer ?? throw new ArgumentNullException(nameof(comparer)));

        private static IEnumerable<KeyValuePair<string, TValue>> EnumerateThrowIfNull<[CanBeNull] TValue>(IEnumerable<KeyValuePair<string, TValue>> items)
        {
            foreach (var item in items)
            {
                if (item.Key == null) throw new ArgumentException("One or more items has a null key.");
                yield return item;
            }
        }

        private static IEnumerable<string> EnumerateThrowIfNull(IEnumerable<string> items)
        {
            foreach (var item in items)
            {
                if (item == null) throw new ArgumentException("One or more items is null.");
                yield return item;
            }
        }
    }

    public enum TemplateInterfaceTestCaseIdentifier : byte
    {
        NoHitCase = 0,
        EnumComparer,
        EnumComparerImpl,
        EnumComparerInstant,
    }

    public static class TemplateInterfaceTestCaseIdentifierExtensions
    {
        public static readonly ImmutableArray<TemplateInterfaceTestCaseIdentifier> DefinedValues;

        public static bool IsDefined(this TemplateInterfaceTestCaseIdentifier id) => DefinedValues.Contains(id);

        public static TemplateInterfaceTestCaseIdentifier
            ValueOrThrowIfNDef(this TemplateInterfaceTestCaseIdentifier id) => id.IsDefined()
            ? id
            : throw new UndefinedEnumArgumentException<TemplateInterfaceTestCaseIdentifier>(id, nameof(id));

        static TemplateInterfaceTestCaseIdentifierExtensions() => DefinedValues = InitDefinedValues();

        private static ImmutableArray<TemplateInterfaceTestCaseIdentifier> InitDefinedValues() =>
            Enum.GetValues<TemplateInterfaceTestCaseIdentifier>().ToImmutableArray();
    }


}
