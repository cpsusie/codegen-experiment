using System;
using System.Collections.Generic;

namespace Cjm.Templates.Test.TemplateInterfaceTestCases
{
    public interface IEnumTotalOrderComparer<TEnum> : IEnumCompleteComparer<TEnum> where TEnum : unmanaged, Enum
    {
        bool Less(TEnum x, TEnum y);
        bool Greater(TEnum x, TEnum y);
        bool LessEq(TEnum x, TEnum y);
        bool GreaterEq(TEnum x, TEnum y);
        new int Compare(TEnum x, TEnum y) => Less(x, y) ? -1 : Less(y, x) ? 1 : 0;
    }

    public interface IEnumCompleteComparer<TEnum> : IEnumEqualityComparer<TEnum>, IEnumComparer<TEnum>
        where TEnum : unmanaged, Enum
    {

    }

    public interface IEnumEqualityComparer<TEnum> : IEqualityComparer<TEnum> where TEnum : unmanaged, Enum
    {
    }

    public interface IEnumComparer<TEnum> : IComparer<TEnum> where TEnum : unmanaged, Enum
    {

    }
}
