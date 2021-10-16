using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Cjm.Templates.Utilities
{
    internal partial struct ImmutableArrayByRefAdapter<T> : IEquatable<ImmutableArrayByRefAdapter<T>>
    {
        public static ImmutableArrayByRefAdapter<T> CreateDestructivelyFromBuilder(ref ImmutableArray<T>.Builder bldr)
        {
            if (bldr == null) throw new ArgumentNullException(nameof(bldr));
            var ret = new ImmutableArrayByRefAdapter<T>(bldr);
            bldr.Clear();
            return ret;
        }

        public readonly int Length => _array.Length;

        public readonly ref readonly T this[int index]
        {
            get
            {
                var arr = _array;
                return ref arr.ItemRef(index);
            }
        }

        public ImmutableArrayByRefAdapter() => _array = ImmutableArray<T>.Empty;

        public ImmutableArrayByRefAdapter(in ImmutableArray<T> wrapMe) => _array = wrapMe.IsDefault ? ImmutableArray<T>.Empty : wrapMe;

        public ImmutableArrayByRefAdapter(IEnumerable<T> src)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));
            _array = src.ToImmutableArray();
        }

        private ImmutableArrayByRefAdapter(ImmutableArray<T>.Builder bldr)
        {
            if (bldr == null) throw new ArgumentNullException(nameof(bldr));
            _array = bldr.Count == bldr.Capacity ? bldr.MoveToImmutable() : bldr.ToImmutable();
            bldr.Clear();
        }

        public readonly Enumerator GetEnumerator() => new (this);

        public readonly override int GetHashCode() => _array.GetHashCode();

        public readonly override bool Equals(object? other) => other switch
        {
            null => false,
            ImmutableArrayByRefAdapter<T> adapter => adapter == this,
            ImmutableArray<T> wrapped => wrapped == _array,
            _ => false,
        };

        public static implicit operator ImmutableArrayByRefAdapter<T>(in ImmutableArray<T> wrapMe) => new(wrapMe);
        public static implicit operator ImmutableArray<T>(in ImmutableArrayByRefAdapter<T> wrapper) => wrapper._array;
        public static bool operator ==(in ImmutableArray<T> lhs, in ImmutableArrayByRefAdapter<T> rhs) => rhs == lhs;
        public static bool operator !=(in ImmutableArray<T> lhs, in ImmutableArrayByRefAdapter<T> rhs) => !(lhs == rhs);
        public static bool operator ==(in ImmutableArrayByRefAdapter<T> lhs, in ImmutableArray<T> rhs) => lhs == (ImmutableArrayByRefAdapter<T>) rhs;
        public static bool operator !=(in ImmutableArrayByRefAdapter<T> lhs, in ImmutableArray<T> rhs) => !(lhs == rhs);
        public static bool operator ==(in ImmutableArrayByRefAdapter<T> lhs, in ImmutableArrayByRefAdapter<T> rhs) =>
            lhs._array == rhs._array;
        public static bool operator !=(in ImmutableArrayByRefAdapter<T> lhs, in ImmutableArrayByRefAdapter<T> rhs) => !(lhs == rhs);
        public readonly bool Equals(ImmutableArrayByRefAdapter<T> other) => other == this;

        /// <inheritdoc />
        public readonly override string ToString() =>
            $"[{nameof(ImmutableArrayByRefAdapter<T>)}] -- Item Count: {_array.Length}";
                

        private ImmutableArray<T> _array;
    }

    partial struct ImmutableArrayByRefAdapter<T>
    {
        public struct Enumerator
        {
            public readonly ref readonly T Current
            {
                get
                {
                    var arr = _arr;
                    return ref arr.ItemRef(_index);
                }
            }

            public Enumerator()
            {
                _arr = ImmutableArray<T>.Empty;
                _index = -1;
            }

            internal Enumerator(ImmutableArrayByRefAdapter<T> owner)
            {
                _arr = owner._array.IsDefault ? ImmutableArray<T>.Empty : owner._array;
                _index = -1;
            }

            public bool MoveNext()
            {
                ++_index;
                return _index > -1 && _index < _arr.Length;
            }

            public void Reset()
            {
                _index = -1;
            }

            private readonly ImmutableArray<T> _arr;
            private int _index;
        }
    }

   
}
