using System;
using System.Collections.Generic;
using System.Text;

namespace Cjm.CodeGen
{
    public delegate bool ByRoRefEqTest<T>(in T lhs, in T rhs) where T : struct, IEquatable<T>;

    public delegate int ByRoRefHasher<T>(in T obj) where T : struct, IEquatable<T>;
    public interface IByRoRefEqualityComparer<T> where T : struct, IEquatable<T>
    {
        bool Equals(in T lhs, in T rhs);
        int GetHashCode(in T val);
    }

    public interface IHasGenericByRefRoEqComparer<TComparer, T> where T : struct, IEquatable<T> where TComparer :  unmanaged, IByRoRefEqualityComparer<T>
    {
        TComparer GetComparer();
    }
}
