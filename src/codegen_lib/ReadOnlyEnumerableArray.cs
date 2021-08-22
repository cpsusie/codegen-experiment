using System;

namespace Cjm.CodeGen
{
    public readonly struct
        ReadOnlyEnumerableArray<TITem> : ISpecificallyRefReadOnlyEnumerable<TITem, RefArrayEnumerator<TITem>>, IEquatable<ReadOnlyEnumerableArray<TITem>>
        where TITem : struct
    {
        public static readonly ReadOnlyEnumerableArray<TITem> InvalidDefault = default;

        public static implicit operator ReadOnlyEnumerableArray<TITem>(TITem[] array) =>
            new(array);

        public static implicit operator TITem[](ReadOnlyEnumerableArray<TITem> arr) =>
            arr != InvalidDefault ? arr._wrappedArray : Array.Empty<TITem>();

        public RefArrayEnumerator<TITem> GetEnumerator() => new (_wrappedArray);

        public ReadOnlyEnumerableArray(TITem[] wrappedArray) =>
            _wrappedArray = wrappedArray ?? throw new ArgumentNullException(nameof(wrappedArray));

        public static bool operator ==(ReadOnlyEnumerableArray<TITem> lhs, ReadOnlyEnumerableArray<TITem> rhs) =>
            ReferenceEquals(lhs._wrappedArray, rhs._wrappedArray);
        public static bool operator !=(ReadOnlyEnumerableArray<TITem> lhs, ReadOnlyEnumerableArray<TITem> rhs) =>
            !(lhs == rhs);

        /// <inheritdoc />
        public override int GetHashCode() => _wrappedArray.GetHashCode();
        public override bool Equals(object? other) => other is ReadOnlyEnumerableArray<TITem> roea && roea == this;
        public bool Equals(ReadOnlyEnumerableArray<TITem> other) => other == this;

        /// <inheritdoc />
        public override string ToString() => $"{TypeName} of {typeof(TITem).Name}, Good: {(this != InvalidDefault)}" +
                                             (this == InvalidDefault ? "." : $"; Length: {_wrappedArray.LongLength}.");
        

        private readonly TITem[] _wrappedArray;
        private static readonly string TypeName = typeof(ReadOnlyEnumerableArray<TITem>).Name;
    }
}