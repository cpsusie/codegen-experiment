using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using Cjm.Templates.Attributes;
using Cjm.Templates.ConstraintSpecifiers;
using Cjm.Templates.ConstraintSpecifiers.OperatorFormSpecifierDelegates;
using Cjm.Templates.Exceptions;
using Cjm.Templates.Utilities;
using HpTimeStamps;

namespace Cjm.Templates.Example
{
    [CjmTemplateInterface]
    [ValueTypeConstraint(ValueTypeConstraintCode.Unmanaged | ValueTypeConstraintCode.Readonly | ValueTypeConstraintCode.NoInstanceFields)]
    public interface ITotalOrderComparisonNullableRefTypeProvider<
        [ReferenceTypeConstraint(ReferenceTypeImplementationConstraintCode.MustBeSealed)]
        [OperatorConstraint(typeof(RoRefValueTypeEqRelCheckOpForm<>), OperatorName.CheckEquals)]
        [OperatorConstraint(typeof(RoRefValueTypeEqRelCheckOpForm<>), OperatorName.GreaterThan)]
        [OperatorConstraint(typeof(RoRefValueTypeEqRelCheckOpForm<>), OperatorName.LessThan)]
        [OperatorConstraint(typeof(RoRefValueTypeEqRelCheckOpForm<>), OperatorName.GreaterThanOrEqual)]
        [OperatorConstraint(typeof(RoRefValueTypeEqRelCheckOpForm<>), OperatorName.LessThanOrEqual)]
        [MustOverride(MustOverrideAttributeTarget.Equals | MustOverrideAttributeTarget.GetHashCode, true)]T> where T : class
    {
        [Pure] public int DefaultNullHashValue { get; }
        [Pure] bool Equals(T? lhs, T? rhs);
        [Pure] int GetHashCode(T? obj);
        [Pure] bool Greater(T? lhs, T? rhs);
        [Pure] bool Less(T? lhs, T? rhs);
        [Pure] bool GreaterEq(T? lhs, T? rhs);
        [Pure] bool LessEq(T? lhs, T? rhs);
        [Pure] int Compare(T? lhs, T? rhs);
    }
    [CjmTemplateInterface]
    [ValueTypeConstraint(ValueTypeConstraintCode.Unmanaged | ValueTypeConstraintCode.Readonly | ValueTypeConstraintCode.NoInstanceFields)]
    public interface ITotalOrderComparisonProviderValueType<
        [FundamentalTypeConstraintVariant(ValueTypeConstraintCode.Unmanaged)]
        [OperatorConstraint(typeof(RoRefValueTypeEqRelCheckOpForm<>), OperatorName.CheckEquals)]
        [OperatorConstraint(typeof(RoRefValueTypeEqRelCheckOpForm<>), OperatorName.GreaterThan)]
        [OperatorConstraint(typeof(RoRefValueTypeEqRelCheckOpForm<>), OperatorName.LessThan)]
        [OperatorConstraint(typeof(RoRefValueTypeEqRelCheckOpForm<>), OperatorName.GreaterThanOrEqual)]
        [OperatorConstraint(typeof(RoRefValueTypeEqRelCheckOpForm<>), OperatorName.LessThanOrEqual)]
        [MustOverride(MustOverrideAttributeTarget.Equals | MustOverrideAttributeTarget.GetHashCode, true)]T> where T : struct
    {
        [Pure]
        bool Equals(T lhs, T rhs);
        [Pure] int GetHashCode(T obj);
        [Pure] bool Greater(T lhs, T rhs);
        [Pure] bool Less(T lhs, T rhs);
        [Pure] bool GreaterEq(T lhs, T rhs);
        [Pure] bool LessEq(T lhs, T rhs);
        [Pure] int Compare(T lhs, T rhs);

        [Pure] bool Equals(in T lhs, in T rhs);
        [Pure] int GetHashCode(in T obj);
        [Pure] bool Greater(in T lhs, in T rhs);
        [Pure] bool Less(in T lhs, in T rhs);
        [Pure] bool GreaterEq(in T lhs, in T rhs);
        [Pure] bool LessEq(in T lhs, in T rhs);
        [Pure] int Compare(in T lhs, in T rhs);

    }

  


    [CjmTemplateInterface]
    [ReferenceTypeConstraint(ReferenceTypeImplementationConstraintCode.MustBeSealed)]
    public interface IBetterListTemplate<
        [FundamentalTypeConstraintVariant(ValueTypeConstraintCode.JustValueType)]
        [OperatorConstraint(typeof(RoRefValueTypeEqRelCheckOpForm<>), OperatorName.CheckEquals)]
        [OperatorConstraint(typeof(RoRefValueTypeEqRelCheckOpForm<>), OperatorName.CheckNotEquals)]
        [OperatorConstraint(typeof(RoRefValueTypeEqRelCheckOpForm<>), OperatorName.GreaterThan)]
        [OperatorConstraint(typeof(RoRefValueTypeEqRelCheckOpForm<>), OperatorName.LessThan)]
        [OperatorConstraint(typeof(RoRefValueTypeEqRelCheckOpForm<>), OperatorName.GreaterThanOrEqual)]
        [OperatorConstraint(typeof(RoRefValueTypeEqRelCheckOpForm<>), OperatorName.LessThanOrEqual)]
        [MustOverride(MustOverrideAttributeTarget.Equals | MustOverrideAttributeTarget.GetHashCode)]
        T, 
        [FundamentalTypeConstraintVariant(ValueTypeConstraintCode.NoInstanceFields | ValueTypeConstraintCode.Readonly | ValueTypeConstraintCode.Unmanaged)]
        TComparer> : IReadOnlyCollection<T> where T : struct where TComparer : unmanaged, ITotalOrderComparisonProviderValueType<T>
    {
        ref T this[int idx] { get; }
        int Capacity { get; }
        void Add(in T value);
        T RemoveAt(int idx);
        int IndexOf(in T value);
        void InsertAt(in T value, int index);
        bool Contains(in T value);
        void AddRange(IEnumerable<T> items);
        void AddRange(in ReadOnlySpan<T> items);
        void Sort();
        bool EnsureCapacity(int newCapacity);
        void TrimExcess();
        Span<T> AsSpan();
        Span<T> Slice(int idx, int length);
        Span<T> Slice(int idx);
        int BinarySearch(in T value);
        void Clear();
    }

    [CjmTemplateImplementation(typeof(IBetterListTemplate<,>))]
    [SuppressMessage("ReSharper", "PossiblyImpureMethodCallOnReadonlyVariable")]
    internal class BetterListImplementation<T, TComparer> : IBetterListTemplate<T, TComparer> where T : struct where TComparer : unmanaged, ITotalOrderComparisonProviderValueType<T>
    {
        public int Capacity => _array.Length;
        public int Count => _count;

        public ref T this[int idx]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (idx > -1 && idx < _count)
                {
                    return ref _array[idx];
                }
                throw new ArgumentOutOfRangeException(nameof(idx), idx,
                    "Parameter must be non-negative and less than the size of the collection.");
            }
        }

        public Enumerator GetEnumerator() => Enumerator.CreateEnumerator(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public void TrimExcess()
        {
            throw new NotImplementedException();
        }

        public Span<T> AsSpan() => _array.AsSpan(0, _count);

        public Span<T> Slice(int startAt)
        {
            if (startAt < 0 || startAt >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(startAt), startAt,
                    "Value must be non-negative and less than the size of the collection.");
            }
            return Slice(startAt, Count - startAt + 1);
        }

        /// <inheritdoc />
        public int BinarySearch(in T value)
        {
            throw new NotImplementedException();
        }

        public Span<T> Slice(int startAt, int length)
        {
            //  0   1   2   3
            //  
            if (startAt < 0 || startAt >= _count)
            {
                throw new ArgumentOutOfRangeException(nameof(startAt), startAt,
                    "Parameter must be non-negative and less than the size of the collection.");
            }

            int tempCap = (Count - startAt + 1);
            if (length < 0)
                throw new ArgumentNegativeException<int>(length, nameof(length));
            if (length > tempCap)
                throw new ArgumentOutOfRangeException(nameof(length), length,
                    $"Parameter \"{nameof(length)}\" (value: {length}) is too great given the starting index (value: {startAt})) and the length of the collection (value: {Count}).");
            return _array.AsSpan(startAt, length);
        }

        public BetterListImplementation()
        {
            _count = 0;
            _array = new T[DefaultSize];
        }

        public BetterListImplementation(int capacity)
        {
            _count = 0;
            _array = capacity switch
            {
                < 0 => throw new ArgumentNegativeException<int>(capacity, nameof(capacity)),
                0 => Array.Empty<T>(),
                _ => new T[capacity]
            };
        }

        public BetterListImplementation(IReadOnlyCollection<T> items) => 
            (_array, _count) = Extract(items ?? throw new ArgumentNullException(nameof(items)));

        public BetterListImplementation(ReadOnlySpan<T> items) => (_array, _count) = Extract(items);

        public BetterListImplementation(IEnumerable<T> source)
        {
            switch (source)
            {
                case null:
                    throw new ArgumentNullException(nameof(source));
                case ImmutableArray<T> immArr:
                    var spanImmut = immArr.AsSpan();
                    (_array, _count) = Extract(spanImmut);
                    break;
                case T[] arr:
                    var span = arr.AsSpan();
                    (_array, _count) = Extract(span);
                    break;
                case IReadOnlyList<T> roList:
                    (_array, _count) = Extract(roList);
                    break;
                default:
                    _array = source.ToArray();
                    _count = _array.Length;
                    break;
            }
        }
        public void Add(T item)
        {
            if (Count + 1 > Capacity)
            {
                T[] arr = GetBiggerArray();
                for (long i = 0; i < Count; ++i)
                {
                    arr[i] = _array[i];
                }

                _array = arr;
            }
            _array[_count++] = item;
        }

        public void Add(in T item)
        {
            if (Count + 1 > Capacity)
            {
                T[] arr = GetBiggerArray();
                for (long i = 0; i < Count; ++i)
                {
                    arr[i] = _array[i];
                }

                _array = arr;
            }
            _array[_count++] = item;
        }

        /// <inheritdoc />
        public T RemoveAt(int idx)
        {
            if (idx < 0)
                throw new ArgumentNegativeException<int>(idx, nameof(idx));
            if (idx >= Count)
                throw new ArgumentOutOfRangeException(nameof(idx), idx,
                    "Parameter must be non-negative and less than the size of the collection.");
            T value = _array[idx];
            for (int i = idx + 1; i < Count; ++i)
            {
                _array[i - 1] = _array[i];
            }
            --_count;
            return value;
        }

        /// <inheritdoc />
        public int IndexOf(in T value)
        {
            for (int i = 0; i < _count; ++i)
            {
                if (TheComparer.Equals(in _array[i], in value))
                    return i;
            }
            return -1;
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

        public bool Contains(in T value) => FirstIndexOf(in value) > -1;

        public int FirstIndexOf(in T value)
        {
            int idx = -1;
            foreach (ref readonly T item in this)
            {
                ++idx;
                if (TheComparer.Equals(in value, in item))
                {
                    return idx;
                }
            }
            return -1;
        }

        public void InsertAt(in T value, int idx)
        {
            if (idx < 0 || idx >= Count)
                throw new ArgumentOutOfRangeException(nameof(idx), idx,
                    $"Value must be non-negative and less than the size of the collection ({Count}).");
            T[] srcArray = _array;
            T[] targetArray = srcArray;
            if (Count + 1 > Capacity)
            {
                targetArray = GetBiggerArray();
                for (int i = 0; i < idx; ++i)
                {
                    targetArray[i] = srcArray[i];
                }
            }

            for (int i = idx; i <= Count; ++i)
            {
                targetArray[i + i] = srcArray[i];
            }

            targetArray[idx] = srcArray[idx];
            _array = targetArray;
            ++_count;
        }

        public void AddRange(IEnumerable<T> items)
        {
            ReadOnlySpan<T> spanToUse;
            switch (items)
            {
                case null:
                    throw new ArgumentNullException(nameof(items));
                case T[] arr:
                    spanToUse = arr;
                    DoAddRange(in spanToUse);
                    break;
                case ImmutableArray<T> arr2:
                    spanToUse = arr2.AsSpan();
                    DoAddRange(in spanToUse);
                    break;
                case ICollection<T> col:
                    DoAddRange(col);
                    break;
                case IReadOnlyCollection<T> roCol:
                    DoAddRange(roCol);
                    break;
                default:
                    foreach (var item in items)
                    {
                        Add(item);
                    }
                    break;
            }
        }

        public void AddRange(in ReadOnlySpan<T> items) => DoAddRange(in items);

        /// <inheritdoc />
        public void Sort()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool EnsureCapacity(int newCapacity)
        {
            if (newCapacity < Count)
                throw new ArgumentOutOfRangeException(nameof(newCapacity), newCapacity,
                    "Parameter must be greater than or equal to the current count.");
            if (newCapacity > Capacity)
            {
                T[] arr = new T[newCapacity];
                Span<T> current = AsSpan();
                current.CopyTo(arr);
                _array = arr;
                return true;
            }
            return false;
        }

        public T RemoveAt(long idx)
        {
            if (idx < 0 || idx >= Count)
                throw new ArgumentOutOfRangeException(nameof(idx), idx,
                    $"Value must be non-negative and less than the size of the collection ({Count}).");

            T ret = _array[idx];
            if (idx < _count - 1)
            {
                for (long i = idx + 1; i < _count; ++i)
                {
                    _array[i - 1] = _array[i];
                }
            }

            --_count;
            return ret;
        }
        public void Clear() => Clear(false);

        public void Clear(bool clearOldArray)
        {
            _count = 0;
            if (clearOldArray)
            {
                _array = new T[DefaultSize];
            }
        }

        private void DoAddRange(in ReadOnlySpan<T> items)
        {
            if (_count + items.Length >= Capacity)
            {
                _array = GetBiggerArray(true);
            }
            Debug.Assert(Capacity > _count + items.Length);
            foreach (ref readonly var item in items)
            {
                _array[_count++] = item;
            }
        }

        private void DoAddRange(IReadOnlyCollection<T> items)
        {
            if (_count + items.Count >= Capacity)
            {
                _array = GetBiggerArray(true);
            }
            Debug.Assert(Capacity > _count + items.Count);
            foreach (var item in items)
            {
                _array[_count++] = item;
            }
        }

        private void DoAddRange(ICollection<T> items)
        {
            if (_count + items.Count >= Capacity)
            {
                _array = GetBiggerArray(true);
            }
            Debug.Assert(Capacity > _count + items.Count);
            foreach (var item in items)
            {
                _array[_count++] = item;
            }
        }

        private T[] GetBiggerArray() => GetBiggerArray(false);

        private T[] GetBiggerArray(bool andCopyCurrent)
        {

            int newCap = CalculateNewCapacity(_array.Length);
            T[] ret = new T[newCap];
            if (andCopyCurrent)
            {
                _array.CopyTo(ret, 0);
            }
            return ret;

            static int CalculateNewCapacity(int currentCapacity)
            {
                int newCapacity;
                try
                {
                    newCapacity = checked ((int)(Math.Ceiling(currentCapacity * ResizeFactor)));
                }
                catch (OverflowException ex)
                {
                    if (currentCapacity == int.MaxValue)
                    {
                        throw new ListFullException(
                            $"The list's current capacity is {currentCapacity}: it cannot be expanded further.", ex);
                    }
                    newCapacity = int.MaxValue;
                }
                return newCapacity;
            }
        }



        private static (T[] Array, int Count) Extract(IReadOnlyCollection<T> items)
        {
            var array = new T[items.Count];
            int idx = 0;
            foreach (var item in items)
            {
                array[idx++] = item;
            }
            return (array, items.Count);
        }

        private static (T[] Array, int Count) Extract(ReadOnlySpan<T> items)
        {
            T[] array;
            int count;
            if (items.IsEmpty)
            {
                count = 0;
                array = Array.Empty<T>();
            }
            else
            {
                array = new T[items.Length];
                for (int i = 0; i < items.Length; ++i)
                {
                    array[i] = items[i];
                }
                count = items.Length;
            }

            return (array, count);
        }

        public struct Enumerator : IEnumerator<T>
        {
            internal static Enumerator CreateEnumerator(BetterListImplementation<T, TComparer> bliImpl) =>
                new(bliImpl ?? throw new ArgumentNullException(nameof(bliImpl)));

            public readonly ref T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    try
                    {
                        return ref _array[_index];
                    }
                    catch (IndexOutOfRangeException ex)
                    {
                        throw new InvalidOperationException(
                            $"The enumerator is not currently in a state where it is valid to access the {nameof(Current)} property.",
                            ex);
                    }
                    catch (NullReferenceException ex)
                    {
                        throw new UninitializedStructAccessException<Enumerator>(this, ex);
                    }
                }
            }

            readonly T IEnumerator<T>.Current => Current;

            readonly object IEnumerator.Current
            {
                get
                {
                    bool ok = _index > -1 && _index < _array.Length;
                    if (!ok)
                    {
                        throw new InvalidOperationException(
                            $"Either the enumerator was not properly " +
                            $"initialized or it is not currently in a state that allows access to its {nameof(Current)} property.");
                    }
                    return Current;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                ++_index;
                return (_index > -1 && _index < _count);
            }

            public void Reset() => _index = -1;

            public void Dispose() {}

            private Enumerator(BetterListImplementation<T, TComparer> bli)
            {
                _array = bli._array ?? throw new ArgumentNullException(nameof(bli._array));
                _count = bli.Count;
                _index = -1;
            }

            private int _index;
            private readonly int _count;
            private readonly T[] _array;
        }

        private static readonly TComparer TheComparer = default;
        private const int DefaultSize = 4;
        private const double ResizeFactor = 1.75;
        private int _count;
        private T[] _array;
    }

    internal readonly struct TotalOrderProviderImplPms : ITotalOrderComparisonProviderValueType<PortableMonotonicStamp>
    {
        public bool Equals(PortableMonotonicStamp lhs, PortableMonotonicStamp rhs) => lhs == rhs;

        /// <inheritdoc />
        public int GetHashCode(PortableMonotonicStamp obj) => obj.GetHashCode();

        public bool Greater(PortableMonotonicStamp lhs, PortableMonotonicStamp rhs) => lhs > rhs;

        public bool Less(PortableMonotonicStamp lhs, PortableMonotonicStamp rhs) => lhs < rhs;

        public bool GreaterEq(PortableMonotonicStamp lhs, PortableMonotonicStamp rhs) => lhs >= rhs;

        public bool LessEq(PortableMonotonicStamp lhs, PortableMonotonicStamp rhs) => lhs <= rhs;

        /// <inheritdoc />
        public int Compare(PortableMonotonicStamp lhs, PortableMonotonicStamp rhs) =>
            PortableMonotonicStamp.Compare(in lhs, in rhs);

        public bool Equals(in PortableMonotonicStamp lhs, in PortableMonotonicStamp rhs) => lhs == rhs;

        /// <inheritdoc />
        public int GetHashCode(in PortableMonotonicStamp obj) => obj.GetHashCode();
        

        public bool Greater(in PortableMonotonicStamp lhs, in PortableMonotonicStamp rhs) => lhs > rhs;

        public bool Less(in PortableMonotonicStamp lhs, in PortableMonotonicStamp rhs) => lhs < rhs;

        public bool GreaterEq(in PortableMonotonicStamp lhs, in PortableMonotonicStamp rhs) => lhs >= rhs;

        public bool LessEq(in PortableMonotonicStamp lhs, in PortableMonotonicStamp rhs) => lhs <= rhs;

        /// <inheritdoc />
        public int Compare(in PortableMonotonicStamp lhs, in PortableMonotonicStamp rhs) =>
            PortableMonotonicStamp.Compare(in lhs, in rhs);

    }
}
