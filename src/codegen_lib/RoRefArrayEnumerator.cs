using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Cjm.CodeGen
{
    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public struct RoRefArrayEnumerator<T> : IByRoRefEnumerator<T>, IEquatable<RoRefArrayEnumerator<T>> where T : struct
    {
        public static readonly RoRefArrayEnumerator<T> InvalidDefault = default;
        public readonly ref readonly T Current => ref _array[_index];
        readonly T INoDisposeEnumerator<T>.Current => Current;
        
        public RoRefArrayEnumerator(T[] array)
        {
            _array = array ?? throw new ArgumentNullException(nameof(array));
            _index = -1;
        }

        public bool MoveNext()
        {
            ++_index;
            return _index > -1 && _index < _array.LongLength;
        }

        public void Reset() => _index = -1;

        public static bool operator ==(in RoRefArrayEnumerator<T> lhs, in RoRefArrayEnumerator<T> rhs) => ReferenceEquals(lhs._array, rhs._array) && lhs._index == rhs._index;

        public static bool operator !=(in RoRefArrayEnumerator<T> lhs, in RoRefArrayEnumerator<T> rhs) => !(lhs == rhs);
        public override readonly int GetHashCode()
        {
            int hash = _array.GetHashCode();
            unchecked
            {
                return (hash * 397) ^ _index.GetHashCode();
            }
        }

        public override readonly bool Equals(object? other) => other is RoRefArrayEnumerator<T> rae && rae == this;
        public readonly bool Equals(RoRefArrayEnumerator<T> other) => other == this;

        public override readonly string ToString() =>
            $"{TypeName} of {ArrayTypeName}, LongLength: {_array.LongLength}, Current Idx: {_index}; " +
            $"Good: {(this != InvalidDefault && _index > -1 && _index < _array.LongLength ? "YES" : "NO")}";

        private readonly T[] _array;
        private long _index;
        private static readonly string ArrayTypeName = typeof(T[]).Name;
        private static readonly string TypeName = typeof(RoRefArrayEnumerator<T>).Name;
    }

    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public struct RoRefImmutArrayEnumerator<T> : IByRoRefEnumerator<T>, IEquatable<RoRefImmutArrayEnumerator<T>> where T : struct
    {
        public static readonly RoRefImmutArrayEnumerator<T> InvalidDefault = default;
        public ref readonly T Current => ref _array.ItemRef(_index);
        readonly T INoDisposeEnumerator<T>.Current => _array[_index];

        public RoRefImmutArrayEnumerator(ImmutableArray<T> array)
        {
            _array = array.IsDefault ? ImmutableArray<T>.Empty : array;
            _index = -1;
        }

        public bool MoveNext()
        {
            ++_index;
            return _index > -1 && _index < _array.Length;
        }

        public void Reset() => _index = -1;
        
        public static bool operator ==(in RoRefImmutArrayEnumerator<T> lhs, in RoRefImmutArrayEnumerator<T> rhs) =>
            lhs._array == rhs._array && lhs._index == rhs._index;

        public static bool operator !=(in RoRefImmutArrayEnumerator<T> lhs, in RoRefImmutArrayEnumerator<T> rhs) => !(lhs == rhs);
        public override readonly int GetHashCode()
        {
            int hash = _array.GetHashCode();
            unchecked
            {
                return (hash * 397) ^ _index.GetHashCode();
            }
        }

        public override readonly bool Equals(object? other) => other is RoRefImmutArrayEnumerator<T> rae && rae == this;
        public readonly bool Equals(RoRefImmutArrayEnumerator<T> other) => other == this;
        public override readonly string ToString() 
            => $"{TypeName} of {ArrayTypeName}, LongLength: {_array.Length}, Current Idx: {_index}; " +
               $"Good: {(this != InvalidDefault && _index > -1 && _index < _array.Length ? "YES" : "NO")}";

        private ImmutableArray<T> _array;
        private int _index;
        private static readonly string ArrayTypeName = typeof(T[]).Name;
        private static readonly string TypeName = typeof(RoRefImmutArrayEnumerator<T>).Name;
    }
}