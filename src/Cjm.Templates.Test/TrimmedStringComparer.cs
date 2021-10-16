using System;
using Cjm.CodeGen;

namespace Cjm.Templates.Test
{
    public sealed class TrimmedStringComparer : StringComparer
    {

        public static TrimmedStringComparer TrimmedOrdinal => TheOrdinalComparer.Value;

        public static TrimmedStringComparer TrimmedOrdinalIgnoreCase => TheOrdinalIgnoreCaseComparer.Value;

        public static TrimmedStringComparer TrimmedCurrentCultureComparer => TheCurrentCultureComparer.Value;

        public static TrimmedStringComparer TrimmedCurrentCultureIgnoreCaseComparer =>
            TheCurrentCultureIgnoreCaseComparer.Value;

        public static TrimmedStringComparer TrimmedInvariantCultureComparer => TheInvariantCultureComparer.Value;

        public static TrimmedStringComparer TrimmedInvariantCultureIgnoreCaseComparer =>
            TheInvariantCultureIgnoreCaseComparer.Value;

        /// <inheritdoc />
        public override int Compare(string? x, string? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(x, null)) return 1;
            if (ReferenceEquals(y, null)) return -1;
            return x.AsSpan().Trim().CompareTo(y.AsSpan().Trim(), _baseComparison);
        }

        /// <inheritdoc />
        public override bool Equals(string? x, string? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) return false;
            return x.AsSpan().Trim().Equals(y.AsSpan().Trim(), _baseComparison);
        }

#if (NETFRAMEWORK || NETSTANDARD2_0_OR_GREATER) && !NETSTANDARD2_1
        /// <inheritdoc />
        public override int GetHashCode(string? obj)
        {
            var span = (obj ?? string.Empty).Trim();
            return BaseComparer.GetHashCode(span);

        }
#else
#pragma warning disable RS1024
        /// <inheritdoc />
        public override int GetHashCode(string? obj)
        {
            var span = (obj ?? string.Empty).AsSpan().Trim();
            return string.GetHashCode(span, _baseComparison);
        }
#pragma warning restore RS1024
#endif

        private StringComparer BaseComparer => _baseComparison switch
        {
            StringComparison.Ordinal => Ordinal,
            StringComparison.OrdinalIgnoreCase => OrdinalIgnoreCase,
            StringComparison.InvariantCulture => InvariantCulture,
            StringComparison.InvariantCultureIgnoreCase => InvariantCultureIgnoreCase,
            StringComparison.CurrentCulture => CurrentCulture,
            StringComparison.CurrentCultureIgnoreCase => CurrentCultureIgnoreCase,
            _ => Ordinal
        };

        private TrimmedStringComparer(StringComparison baseComp) => _baseComparison = baseComp;

        private readonly StringComparison _baseComparison;

        private static readonly LocklessLazyWriteOnce<TrimmedStringComparer> TheOrdinalComparer =
            new(() => InitComparer(StringComparison.Ordinal));
        private static readonly LocklessLazyWriteOnce<TrimmedStringComparer> TheOrdinalIgnoreCaseComparer = new(
            () => InitComparer(StringComparison.OrdinalIgnoreCase));
        private static readonly LocklessLazyWriteOnce<TrimmedStringComparer> TheCurrentCultureComparer =
            new(() =>
                new TrimmedStringComparer(StringComparison.CurrentCulture));
        private static readonly LocklessLazyWriteOnce<TrimmedStringComparer> TheCurrentCultureIgnoreCaseComparer =
            new(() =>
                new TrimmedStringComparer(StringComparison.CurrentCultureIgnoreCase));

        private static readonly LocklessLazyWriteOnce<TrimmedStringComparer> TheInvariantCultureComparer =
            new(() =>
                new TrimmedStringComparer(StringComparison.InvariantCulture));
        private static readonly LocklessLazyWriteOnce<TrimmedStringComparer> TheInvariantCultureIgnoreCaseComparer =
            new(() =>
                new TrimmedStringComparer(StringComparison.InvariantCultureIgnoreCase));



        private static TrimmedStringComparer InitComparer(StringComparison baseComp) =>
            new(baseComp);
    }
}
