using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cjm.CodeGen
{
    public delegate TOutput TransformRoRefIn<TInput, TOutput>(in TInput input) where TInput : struct;

    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public sealed class GenerateEnumeratorAttribute : Attribute
    {

    }

    [GenerateEnumerator]
    public readonly partial struct RoRefInTransformEnumerable<TRoRefEnumerable, TRoRefEnumerator, TItemIn, TItemOut>
        where TRoRefEnumerator : struct, IByRoRefEnumerator<TItemIn>
        where TRoRefEnumerable : ISpecificallyRefReadOnlyEnumerable<TItemIn, TRoRefEnumerator>
        where TItemIn : struct
    {
        

        private readonly TransformRoRefIn<TItemIn, TItemOut> _transformation;
        private readonly TRoRefEnumerable _enumerable;
    }
}
