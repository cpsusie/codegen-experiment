using System;
using System.Collections;
using System.Collections.Generic;

namespace Cjm.CodeGen
{
    public struct RefArrayEnumerator<T> : IByRefEnumerator<T>, IEquatable<RefArrayEnumerator<T>> where T : struct
    {
        public static readonly RefArrayEnumerator<T> InvailidDefault = default;
        public readonly ref T Current => ref _array[_index];
        readonly ref readonly T IByRoRefEnumerator<T>.Current => ref Current;
        readonly T IEnumerator<T>.Current => Current;
        readonly object IEnumerator.Current
        {
            get
            {
                if (_index > -1 && _index < _array.LongLength)
                    return Current;
                throw new InvalidOperationException("The enumerator is not in a proper state to retrieve the current property.");
            }
        }

        public RefArrayEnumerator(T[] array)
        {
            _array = array ?? throw new ArgumentNullException(nameof(array));
            _index = -1;
        }

        public bool MoveNext()
        {
            ++_index;
            return _index > -1 && _index < _array.LongLength;
        }

        public void Reset()
        {
            _index = -1;
        }

        public void Dispose() { }

        public static bool operator ==(in RefArrayEnumerator<T> lhs, in RefArrayEnumerator<T> rhs) => ReferenceEquals(lhs._array, rhs._array) && lhs._index == rhs._index;

        public static bool operator !=(in RefArrayEnumerator<T> lhs, in RefArrayEnumerator<T> rhs) => !(lhs == rhs);
        public override readonly int GetHashCode()
        {
            int hash = _array.GetHashCode();
            unchecked
            {
                return (hash * 397) ^ _index.GetHashCode();
            }
        }

        public  override readonly bool Equals(object? other) => other is RefArrayEnumerator<T> rae && rae == this;
        public readonly bool Equals(RefArrayEnumerator<T> other) => other == this;

        public override readonly string ToString() =>
            $"{TypeName} of {ArrayTypeName}, LongLength: {_array.LongLength}, " +
            $"Current Idx: {_index}; " +
            $"Good: {(this != InvailidDefault && _index > -1 && _index < _array.LongLength ? "YES" : "NO")}";

        private readonly T[] _array;
        private long _index;
        private static readonly string ArrayTypeName = typeof(T[]).Name;
        private static readonly string TypeName = typeof(RefArrayEnumerator<T>).Name;
    }
}