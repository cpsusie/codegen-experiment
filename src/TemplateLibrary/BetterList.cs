using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TemplateLibrary
{
    public delegate bool RefEqualityTester<T>(in T l, in T r) where T : struct;

    public delegate bool EqualityTester<T>(T l, T r) where T : struct;

    

    public sealed class BetterList<T> : ISpecificallyRefEnumerable<T, BetterList<T>.Enumerator> where T : struct
    {
        public long Count => _count;
        public long Capacity => _array.LongLength;
        public ref T this[long index] =>
            ref _array[
                index > -1 && index < Count
                    ? index
                    : throw new ArgumentOutOfRangeException(nameof(index), index,
                        $"Index must be non-negative and less than {Count}.")];

        public Span<T> AsSpan() => _array.AsSpan();

        public Span<T> Slice(int idx, int length) => _array.AsSpan(idx, length);

        public Enumerator GetEnumerator() => new Enumerator(this);

        public BetterList()
        {
            _array = new T[DefaultSize];
            _count = 0;
        }

        public BetterList(long capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Value must non-negative.");
            _array = new T[capacity];
            _count = 0;
        }

        public BetterList(IEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            (T[] array, long capcity, long count) = (source) switch
            {
                BetterList<T> bl =>  (bl._array.ToArray(), bl._array.LongLength, bl._count),
                T[] arr => (arr, arr.LongLength, arr.LongLength),
                IReadOnlyCollection<T> roc => (roc.ToArray(), roc.Count, roc.Count),
                _ => ToArrayWithLengthHardWay(source)    
            };

            _array = array;
            _count = count;
            Debug.Assert(_count <= _array.LongLength);

            (T[] Array, long Capacity, long Count) ToArrayWithLengthHardWay(IEnumerable<T> src)
            {
                T[] arr = src.ToArray();
                return (arr, arr.LongLength, arr.LongLength);
            }
            
        }

        public void Add(T item)
        {
            if (Count + 1 > Capacity)
            {
                long newCapacity;
                checked
                {
                    newCapacity = (long)Math.Ceiling(Count * 1.75);
                    Debug.Assert(newCapacity > Capacity && newCapacity >= Count + 1);
                }

                T[] arr = new T[newCapacity];
                for (long i = 0; i < Count; ++i)
                {
                    arr[i] = _array[i];
                }

                _array = arr;
            }
            _array[++_count] = item;
        }

        public void Add(in T item)
        {
            if (Count + 1 > Capacity)
            {
                long newCapacity;
                checked
                {
                    newCapacity = (long)Math.Ceiling(Count * 1.75);
                    Debug.Assert(newCapacity > Capacity && newCapacity >= Count + 1);
                }

                T[] arr = new T[newCapacity];
                for (long i = 0; i < Count; ++i)
                {
                    arr[i] = _array[i];
                }

                _array = arr;
            }
            _array[++_count] = item;
        }

        public (bool Popped, T? PoppedValue) RemoveLast()
        {
            if (Count == 0)
            {
                return (false, default);
            }

            ref readonly T val = ref _array[_count-- - 1];
            return (true, val);
        }

        public bool Contains(in T value, RefEqualityTester<T> eqTester) => FirstIndexOf(in value, eqTester) > -1;

        public int FirstIndexOf(in T value, RefEqualityTester<T> eqTester)
        {
            if (eqTester == null) throw new ArgumentNullException(nameof(eqTester));
            int idx = -1;
            foreach (ref readonly T item in this)
            {
                ++idx;
                if (eqTester(in value, in item))
                {
                    return idx;
                }
            }
            return -1;
        }

        public struct Enumerator : IByRefEnumerator<T>
        {
            public readonly ref T Current => ref _ownerArray[_index];

            /// <inheritdoc />
            T INoDisposeEnumerator<T>.Current => Current;

            /// <inheritdoc />
            ref readonly T IByRoRefEnumerator<T>.Current => ref Current;

            public bool MoveNext()
            {
                ++_index;
                return _index < _ownerArray.Length && _index > -1;
            }

            public void Reset()
            {
                _index = -1;
            }

            internal Enumerator(BetterList<T> owner)
            {
                _ownerArray = owner._array;
                _index = -1;
            }

            private readonly T[] _ownerArray;
            private long _index;
        }

        private T[] _array;
        private long _count;
        private const long DefaultSize = 8;

    }
}
