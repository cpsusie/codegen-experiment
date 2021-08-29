using System;
using System.Collections.Generic;

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
}