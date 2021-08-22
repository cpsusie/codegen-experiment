using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codegen_lib
{

    

    public interface IByRoRefEnumerator<T> : IEnumerator<T>
    {
        new  ref readonly T Current {  get; }        
    }

    public interface IByRefEnumerator<T> : IByRoRefEnumerator<T>
    {
        new ref T Current {  get; }
    }

    public interface ISpecificallyStructEnumerable<TItem,  TEnumerator> where TEnumerator : struct, IEnumerator<TItem>
    {
        TEnumerator GetEnumerator();
    } 

    

    public interface ISpecificallyRefEnumerable<TItem, TEnumerator> where TEnumerator : struct, IEnumerator<TItem>
    {

    }

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
        public readonly override int GetHashCode()
        {
            int hash = _array.GetHashCode();
            unchecked
            {
                return (hash * 397) ^ _index.GetHashCode();
            }
        }

        public readonly override bool Equals(object? other) => other is RoRefArrayEnumerator<T> rae && rae == this;
        public readonly bool Equals(RoRefArrayEnumerator<T> other) => other == this;
        public readonly override string ToString() => $"{TypeName} of {ArrayTypeName}, LongLength: {_array.LongLength}, Current Idx: {_index}; Good: {(_array != null && _index > -1 && _index < _array.LongLength ? "YES" : "NO")}";

        private readonly T[] _array;
        private long _index;
        private static readonly string ArrayTypeName = typeof(T[]).Name;
        private static readonly string TypeName = typeof(RoRefArrayEnumerator<T>).Name;
    }

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
        public readonly override int GetHashCode()
        {
            int hash = _array.GetHashCode();
            unchecked
            {
                return (hash * 397) ^ _index.GetHashCode();
            }
        }

        public readonly override bool Equals(object? other) => other is RefArrayEnumerator<T> rae && rae == this;
        public readonly bool Equals(RefArrayEnumerator<T> other) => other == this;
        public readonly override string ToString() => $"{TypeName} of {ArrayTypeName}, LongLength: {_array.LongLength}, Current Idx: {_index}; Good: {(_array != null && _index > -1 && _index < _array.LongLength ? "YES" : "NO")}";

        private readonly T[] _array;
        private long _index;
        private static readonly string ArrayTypeName = typeof(T[]).Name;
        private static readonly string TypeName = typeof(RefArrayEnumerator<T>).Name;
    }

    public struct ListOfTEnumerable<T> : ISpecificallyRefEnumerable<T, List<T>.Enumerator>
    {
        public readonly List<T>.Enumerator GetEnumerator() => _wrapped.GetEnumerator();

        public ListOfTEnumerable(List<T> list) => _wrapped = list ?? throw new ArgumentNullException(nameof(list)); 

        private readonly List<T> _wrapped;
    }
}
