using System.Collections.Generic;
// ReSharper disable NonReadonlyMemberInGetHashCode

namespace Cjm.CodeGen
{
    public interface IByRoRefEnumerator<T> : IEnumerator<T>
    {
        new  ref readonly T Current {  get; }        
    }

    public interface IByRefEnumerator<T> : IByRoRefEnumerator<T>
    {
        new ref T Current {  get; }
    }

    public interface ISpecificallyStructEnumerable<TItem,  TEnumerator> where TEnumerator : struct, IEnumerator<TItem>
    {
        TEnumerator GetEnumerator();
    } 

    

    public interface ISpecificallyRefEnumerable<TItem, TEnumerator> where TEnumerator : struct, IByRefEnumerator<TItem>
    {
        TEnumerator GetEnumerator();
    }

    public interface ISpecificallyRefReadOnlyEnumerable<TItem, TEnumerator>
        where TEnumerator : struct, IByRoRefEnumerator<TItem>
    {
        TEnumerator GetEnumerator();
    }
}
