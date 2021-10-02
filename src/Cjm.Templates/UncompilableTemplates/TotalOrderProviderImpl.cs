using System.Runtime.CompilerServices;
using Cjm.Templates.Attributes;

namespace Cjm.Templates.Example
{
    [CjmTemplateImplementation(typeof(ITotalOrderComparisonProviderValueType<>))]
    internal readonly struct TotalOrderProviderImpl<T> : ITotalOrderComparisonProviderValueType<T> where T : struct
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(T lhs, T rhs) => lhs == rhs;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetHashCode(T obj) => obj.GetHashCode();
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Greater(T lhs, T rhs) => lhs > rhs;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Less(T lhs, T rhs) => lhs < rhs;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GreaterEq(T lhs, T rhs) => lhs >= rhs;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool LessEq(T lhs, T rhs) => lhs <= rhs;

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(T lhs, T rhs) => GreaterEq(lhs, rhs) ? Greater(lhs, rhs) ? 1 : 0 : -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(in T lhs, in T rhs) => lhs == rhs;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetHashCode(in T obj) => obj.GetHashCode();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Greater(in T lhs, in T rhs) => lhs > rhs;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Less(in T lhs, in T rhs) => lhs < rhs;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GreaterEq(in T lhs, in T rhs) => lhs >= rhs;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool LessEq(in T lhs, in T rhs) => lhs <= rhs;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(in T lhs, in T rhs) => GreaterEq(in lhs, in rhs) ? (Greater(in lhs, in rhs) ? 1 : 0) : -1;

    }
}