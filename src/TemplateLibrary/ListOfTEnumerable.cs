using System;
using System.Collections.Generic;
using HpTimeStamps;
namespace TemplateLibrary
{
    public readonly struct ListOfTEnumerable<T> : ISpecificallyStructEnumerable<T, List<T>.Enumerator>
    {
        public static implicit operator ListOfTEnumerable<T>(List<T> source) => new(source);

        public static implicit operator List<T>(ListOfTEnumerable<T> source) => source._wrapped ??
            throw new InvalidOperationException(
                $"An uninitialized object of type {nameof(ListOfTEnumerable<T>)} cannot be converted to a {nameof(List<T>)}.");

        public List<T>.Enumerator GetEnumerator() => _wrapped.GetEnumerator();

        public ListOfTEnumerable(List<T> list) => _wrapped = list ?? throw new ArgumentNullException(nameof(list)); 

        private readonly List<T> _wrapped;
    }

    public readonly struct ListOfPortableTimeStampWrapper : ISpecificallyStructEnumerable<PortableMonotonicStamp, List<PortableMonotonicStamp>.Enumerator>
    {
        public List<PortableMonotonicStamp>.Enumerator GetEnumerator() => _wrapped.GetEnumerator();

        public ListOfPortableTimeStampWrapper(List<PortableMonotonicStamp> wrapMe) => _wrapped = wrapMe;

        private readonly ListOfTEnumerable<PortableMonotonicStamp> _wrapped;
    }

    public readonly struct ArrayOfPortableMonotonicStampWrapper : ISpecificallyRefReadOnlyEnumerable<
        PortableMonotonicStamp, RoRefArrayEnumerator<PortableMonotonicStamp>>
    {
        public static implicit operator ArrayOfPortableMonotonicStampWrapper(PortableMonotonicStamp[] stamps) =>
            new((stamps ?? throw new ArgumentNullException(nameof(stamps))));
        
        /// <inheritdoc />
        public RoRefArrayEnumerator<PortableMonotonicStamp> GetEnumerator() => _wrapper.GetEnumerator(); 

        public ref readonly PortableMonotonicStamp this[long idx] => ref _wrapper[idx];

        public ref readonly PortableMonotonicStamp this[Index idx] => ref _wrapper[idx];

        public ref readonly PortableMonotonicStamp this[int idx] => ref _wrapper[idx];

        private ArrayOfPortableMonotonicStampWrapper(ReadOnlyEnumerableArray<PortableMonotonicStamp> items) =>
            _wrapper = items == default ? throw new ArgumentException("Parameter not initialized.", nameof(items)) : items;

        private readonly ReadOnlyEnumerableArray<PortableMonotonicStamp> _wrapper;
    }
}