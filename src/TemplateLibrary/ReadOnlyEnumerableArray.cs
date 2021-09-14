using System;

namespace TemplateLibrary
{
    public readonly struct
        ReadOnlyEnumerableArray<TITem> : ISpecificallyRefReadOnlyEnumerable<TITem, RoRefArrayEnumerator<TITem>>, IEquatable<ReadOnlyEnumerableArray<TITem>>
            where TITem : struct
    {
        public static readonly ReadOnlyEnumerableArray<TITem> InvalidDefault = default;

        public static implicit operator ReadOnlyEnumerableArray<TITem>(TITem[] array) =>
            new(array);

        public static implicit operator TITem[](ReadOnlyEnumerableArray<TITem> arr) =>
            arr != InvalidDefault ? arr._wrappedArray : Array.Empty<TITem>();

        public ref readonly TITem this[long idx] => ref _wrappedArray[idx];
        public ref readonly TITem this[Index idx] => ref _wrappedArray[idx];
        public ref readonly TITem this[int idx] => ref _wrappedArray[idx];

        public RoRefArrayEnumerator<TITem> GetEnumerator() => new (_wrappedArray);

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