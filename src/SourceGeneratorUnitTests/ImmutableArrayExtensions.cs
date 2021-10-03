using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Cjm.Templates.Test")]
namespace SourceGeneratorUnitTests
{
    internal delegate void RefActionRt<T>(ref T? item) where T : class;

    internal delegate void RefActionVt<T>(ref T val) where T : struct;

    internal delegate void RefActionNVt<T>(ref T? val) where T : struct;

    internal delegate void RoRefActVt<T>(in T val) where T : struct;

    internal static class ImmutableArrayExtensions
    {
        internal static void ApplyToAll<T>(this ImmutableArray<T> immutArr, Action<T> a)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));
            if (immutArr.IsDefault)
                throw new ArgumentException(@"The parameter has not been properly initialized.", nameof(immutArr));
            foreach (var item in immutArr)
            {
                a(item);
            }
        }

        internal static void ApplyToAllByRef<T>(this ImmutableArray<T> immutArr, RoRefActVt<T> action) where T : struct
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (immutArr.IsDefault)
                throw new ArgumentException(@"The parameter has not been properly initialized.", nameof(immutArr));
            for (int i = 0; i < immutArr.Length; ++i)
            {
                action(in immutArr.ItemRef(i));
            }
        }
    }
}