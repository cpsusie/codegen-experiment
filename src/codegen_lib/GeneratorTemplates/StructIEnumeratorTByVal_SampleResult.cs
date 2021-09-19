using System;
using System.Collections;
using System.Collections.Generic;


namespace FromDoodle.Cjm.Test
{
    partial class AnotherProgram
    {
        public struct WrappedListOfPortableMonotonicStampByVal
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            public readonly bool IsDefault => _wrapped == default;

            public static implicit operator WrappedListOfPortableMonotonicStampByVal(List<HpTimeStamps.PortableMonotonicStamp> collection) =>
                new(collection);

            public static implicit operator List<HpTimeStamps.PortableMonotonicStamp>(WrappedListOfPortableMonotonicStampByVal stuff) => stuff._wrapped ??
                throw new InvalidOperationException("Collection has not been initialized.");

            public StructIEnumeratorTByVal GetEnumerator() => new(_wrapped.GetEnumerator());

            //CTOR
            private WrappedListOfPortableMonotonicStampByVal(List<HpTimeStamps.PortableMonotonicStamp> col) => _wrapped = col ?? throw new ArgumentNullException(nameof(col));


            private readonly List<HpTimeStamps.PortableMonotonicStamp> _wrapped;

            public struct StructIEnumeratorTByVal : IDisposable
            {
                //Case is readonly member
                public readonly HpTimeStamps.PortableMonotonicStamp Current => _wrapped.Current;

                public bool MoveNext() => _wrapped.MoveNext();

                internal StructIEnumeratorTByVal(System.Collections.Generic.List<HpTimeStamps.PortableMonotonicStamp>.Enumerator enumerator) => _wrapped = enumerator;



                public void Dispose() => _wrapped.Dispose();

                private System.Collections.Generic.List<HpTimeStamps.PortableMonotonicStamp>.Enumerator _wrapped;
            }
        }
    }
}