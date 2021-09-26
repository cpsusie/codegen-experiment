using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Cjm.Templates.Attributes;
using Cjm.Templates.ConstraintSpecifiers;
using Cjm.Templates.ConstraintSpecifiers.OperatorFormSpecifierDelegates;
using Cjm.Templates.Exceptions;
using Cjm.Templates.Utilities;

namespace Cjm.Templates.Example
{
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
        T> : IReadOnlyCollection<T> where T : struct
    {
        ref T this[int idx] { get; }
        int Capacity { get; }
        void Add(in T value);
        T RemoveAt(int idx);
        int IndexOf(in T value);
        void InsertAt(in T value, int index);
        bool Contains(in T value);
        void AddRange(IEnumerable<T> items);
        void Sort();
        bool EnsureCapacity(int newCapacity);
        void TrimExcess();
        Span<T> AsSpan();
        Span<T> Slice(int idx, int length);
        Span<T> Slice(int idx);
        int BinarySearch(in T value);
        void Clear();
    }

    [CjmTemplateImplementation(typeof(IBetterListTemplate<>))]
    internal class BetterListImplementation<T> : IBetterListTemplate<T> where T : struct 
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
                    $"Parameter \"{nameof(length)}\" (value: {length}) is too great given the starting index (value: {startAt})) and the length of the collection (value: {Count}).";
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

        public void Clear() => Clear(false);

        public void Clear(bool clearOldArray)
        {
            _count = 0;
            if (clearOldArray)
            {
                _array = new T[DefaultSize];
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
            internal static Enumerator CreateEnumerator(BetterListImplementation<T> bliImpl) =>
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
                    bool ok = _index > -1 && _index < (_array?.Length ?? -1);
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

            private Enumerator(BetterListImplementation<T> bli)
            {
                _array = bli._array ?? throw new ArgumentNullException(nameof(bli._array));
                _count = bli.Count;
                _index = -1;
            }

            private int _index;
            private readonly int _count;
            private readonly T[] _array;
        }

        private const int DefaultSize = 4;
        private int _count;
        private T[] _array;
    }
}
