using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Cjm.Templates.Utilities.SetOnce;

namespace Cjm.Templates
{
    namespace System.Runtime.CompilerServices
    {
        public class IsExternalInit { }
    }

    public static class AdditionalTemplatesRepository
    {
        public static bool AreTemplatesFixedNow => TheItems.IsSet;

        public static ImmutableArray<string> AdditionalTemplates => TheItems.Value;
        
        static AdditionalTemplatesRepository()
        {
            TheItems = new LocklessLazyWriteOnceValueType<ImmutableArray<string>>(InitItems);
        }

        public static bool TrySupplyAlternateTemplates(IEnumerable<string> others) =>
            TrySupplyAlternateTemplates(others, true);

        public static void SupplyAlternateTemplatesOrThrow(IEnumerable<string> others) =>
            SupplyAlternateTemplatesOrThrow(others, true);
        
        public static bool TrySupplyAlternateTemplates(IEnumerable<string> others, bool includeStandard)
        {
            if (others == null) throw new ArgumentNullException(nameof(others));
            ImmutableArray<string> newVal = includeStandard
                ? StandardItems().Concat(others.Where(itm => !string.IsNullOrWhiteSpace(itm))).ToImmutableArray()
                : StandardItems().ToImmutableArray();
            return TheItems.TrySet(newVal);
        }

        public static void SupplyAlternateTemplatesOrThrow(IEnumerable<string> others, bool includeStandard)
        {
            if (!TrySupplyAlternateTemplates(others, includeStandard))
            {
                throw new InvalidOperationException(
                    "The value was already set or was being set on another thread at moment of call.");
            }
        }

        private static ImmutableArray<string> InitItems() => StandardItems().ToImmutableArray();
        
        private static IEnumerable<string> StandardItems()
        {
            yield return UncompileableTemplates.TotalOrderProviderImpl;
        }

        private static readonly LocklessLazyWriteOnceValueType<ImmutableArray<string>> TheItems;
    }


}
