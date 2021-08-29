using System.Collections.Generic;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace TemplateLibrary
{
    public interface INoDisposeEnumerator<out T>
    {
        T Current { get; }
        bool MoveNext();
        void Reset();
    }

    public interface IByRoRefEnumerator<T>  : INoDisposeEnumerator<T>
    {
        new ref readonly T Current {  get; }

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

    public interface ISpecificallyClassEnumerable<TItem, TEnumerator> where TEnumerator : class, IEnumerator<TItem> 
    {
        TEnumerator GetEnumerator();
    }
}
