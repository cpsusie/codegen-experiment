using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Cjm.Templates.Attributes;
using Cjm.Templates.Example;

namespace Cjm.Templates.Test.TemplateInterfaceTestCases
{
    [CjmTemplateImplementation(typeof(IEnumComparer<>))]
    internal readonly struct EnumComparerUncompilableImpl<TEnum> : IEnumComparer<TEnum> where TEnum : unmanaged, Enum
    {

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(TEnum x, TEnum y) => x == y;
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetHashCode(TEnum val) => val.GetHashCode();


        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Less(TEnum x, TEnum y) => x < y;


        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Greater(TEnum x, TEnum y) => x > y;


        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool LessOrEq(TEnum x, TEnum y) => x <= y;


        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GreaterOrEq(TEnum x, TEnum y) => x >= y;

    }
}
