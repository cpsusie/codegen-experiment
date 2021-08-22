using System;
using System.Collections.Generic;

namespace Cjm.CodeGen
{
    public readonly struct ListOfTEnumerable<T> : ISpecificallyStructEnumerable<T, List<T>.Enumerator>
    {
        public readonly List<T>.Enumerator GetEnumerator() => _wrapped.GetEnumerator();

        public ListOfTEnumerable(List<T> list) => _wrapped = list ?? throw new ArgumentNullException(nameof(list)); 

        private readonly List<T> _wrapped;
    }
}