using System;
using System.Runtime.CompilerServices;
using TItem = HpTimeStamps.PortableMonotonicStamp;
using TWrappedEnumerator = System.Collections.Generic.List<HpTimeStamps.PortableMonotonicStamp>.Enumerator;
using TWrappedCollection = System.Collections.Generic.List<HpTimeStamps.PortableMonotonicStamp>;

//0: needed usings
//1: namespace name
//2: static class to augment name
//3: "public" or "internal"
//4: CollectionWrapperName
//5: TWrappedCollection
//6: CTOR code
//7: "readonly" if wrapped coll is ref type, string.Empty otherwise
//8: Item name
//9: WrappedEnumerator type
//10: Reset Method
//11: Dispose method
//12: : IDisposable or string.Empty
namespace Cjm.CodeGen.GeneratorTemplates
{
    internal static class Constants
    {
        public const string MoveNextReadonlyToken = "readonly";

        public const bool ImplementDisposePublicly = true;
        
        public const bool ImplementResetPublicly = false;

        public const bool WrappedCollectionIsValueType = false;
    }
    
}


namespace Cjm.CodeGen.GeneratorTemplates
{

    public struct WrappedListByVal
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        public readonly bool IsDefault => _wrapped == default;

        public static implicit operator WrappedListByVal(TWrappedCollection collection) =>
            new WrappedListByVal(collection);

        public static implicit operator TWrappedCollection(WrappedListByVal stuff) => stuff._wrapped ??
            throw new InvalidOperationException("Collection has not been initialized.");
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StructIEnumeratorTByVal GetEnumerator() => new(_wrapped.GetEnumerator());

        //CASE: WrappedCollectionIsValueType
        //private WrappedListByVal(TWrappedCollection col)
        //{
        //    _wrapped = col;
        //}

        //Case: !WrappedCollectionIsValueType
        private WrappedListByVal(TWrappedCollection col) =>
            _wrapped = col ?? throw new ArgumentNullException(nameof(col));

        private readonly /*readonly <=> WrappedCollectionIsValueType*/ TWrappedCollection _wrapped;

        public struct StructIEnumeratorTByVal : IEnumerator<TItem>
        {
            //Case is readonly member
            public readonly TItem Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return _wrapped.Current;
                }
            }

            readonly object IEnumerator.Current => Current;
            //end case

            // Case is not readonly member
            //public TItem Current => _wrapped.Current;
            //object IEnumerator.Current => Current;
            //End Case
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => _wrapped.MoveNext();

            internal StructIEnumeratorTByVal(TWrappedEnumerator enumerator) => _wrapped = enumerator;

            //Case: Implement Reset Publicly
            //public void Reset() => _wrapped.Reset();

            //Case: !Implement Reset Publicly
            void IEnumerator.Reset() => ((IEnumerator)_wrapped).Reset();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            //Case implement dispose publicly
            public void Dispose() => _wrapped.Dispose(); 

            private TWrappedEnumerator _wrapped;
        }
    }

    
}
