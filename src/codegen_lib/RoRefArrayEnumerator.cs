using System;
using System.Collections;
using System.Collections.Generic;

namespace Cjm.CodeGen
{
    public struct RoRefArrayEnumerator<T> : IByRoRefEnumerator<T>, IEquatable<RoRefArrayEnumerator<T>> where T : struct
    {
        public static readonly RoRefArrayEnumerator<T> InvailidDefault = default;
        public readonly ref readonly T Current => ref _array[_index];
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

        public void Reset()
        {
            _index = -1;
        }

        public void Dispose() { }

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
        public override readonly string ToString() => $"{TypeName} of {ArrayTypeName}, LongLength: {_array.LongLength}, Current Idx: {_index}; Good: {(this != InvailidDefault && _index > -1 && _index < _array.LongLength ? "YES" : "NO")}";

        private readonly T[] _array;
        private long _index;
        private static readonly string ArrayTypeName = typeof(T[]).Name;
        private static readonly string TypeName = typeof(RoRefArrayEnumerator<T>).Name;
    }
}