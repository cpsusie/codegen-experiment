using System;

namespace Cjm.CodeGen
{
    public readonly struct
        RefEnumerableArray<TITem> : ISpecificallyRefEnumerable<TITem, RefArrayEnumerator<TITem>>, IEquatable<RefEnumerableArray<TITem>>
        where TITem : struct
    {
        public static readonly RefEnumerableArray<TITem> InvalidDefault = default;

        public static implicit operator ReadOnlyEnumerableArray<TITem>(RefEnumerableArray<TITem> arr) =>
            arr != InvalidDefault
                ? new ReadOnlyEnumerableArray<TITem>(arr._wrappedArray)
                : new ReadOnlyEnumerableArray<TITem>(Array.Empty<TITem>());

        public static implicit operator RefEnumerableArray<TITem>(TITem[] array) =>
            new(array);

        public static implicit operator TITem[](RefEnumerableArray<TITem> arr) =>
            arr != InvalidDefault ? arr._wrappedArray : Array.Empty<TITem>();

        public RefArrayEnumerator<TITem> GetEnumerator() => new (_wrappedArray);

        public RefEnumerableArray(TITem[] wrappedArray) =>
            _wrappedArray = wrappedArray ?? throw new ArgumentNullException(nameof(wrappedArray));

        public static bool operator ==(RefEnumerableArray<TITem> lhs, RefEnumerableArray<TITem> rhs) =>
            ReferenceEquals(lhs._wrappedArray, rhs._wrappedArray);
        public static bool operator !=(RefEnumerableArray<TITem> lhs, RefEnumerableArray<TITem> rhs) =>
            !(lhs == rhs);

        /// <inheritdoc />
        public override int GetHashCode() => _wrappedArray.GetHashCode();
        public override bool Equals(object? other) => other is RefEnumerableArray<TITem> roea && roea == this;
        public bool Equals(RefEnumerableArray<TITem> other) => other == this;

        /// <inheritdoc />
        public override string ToString() => $"{TypeName} of {typeof(TITem).Name}, Good: {(this != InvalidDefault)}" +
                                             (this == InvalidDefault ? "." : $"; Length: {_wrappedArray.LongLength}.");


        private readonly TITem[] _wrappedArray;
        private static readonly string TypeName = typeof(RefEnumerableArray<TITem>).Name;
    }
}