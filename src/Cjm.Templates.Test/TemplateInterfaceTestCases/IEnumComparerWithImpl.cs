using System;
using Cjm.Templates.Attributes;
using Cjm.Templates.ConstraintSpecifiers;

namespace Cjm.Templates.Test.TemplateInterfaceTestCases
{
    [CjmTemplateInterface]
    [FundamentalTypeConstraintVariant(ValueTypeConstraintCode.Readonly | ValueTypeConstraintCode.Unmanaged | ValueTypeConstraintCode.NoInstanceFields)]
    public interface IEnumComparer<[FundamentalTypeConstraintVariant(EnumConstraintType.AnyConcreteEnum)] TEnum> where TEnum : unmanaged, Enum
    {
        bool Equals(TEnum x, TEnum y);
        int GetHashCode(TEnum val);
        bool Less(TEnum x, TEnum y);
        bool Greater(TEnum x, TEnum y);
        bool LessOrEq(TEnum x, TEnum y);
        bool GreaterOrEq(TEnum x, TEnum y);
        int Compare(TEnum x, TEnum y) => Less(x, y) ? -1 : Less(y, x) ? 1 : 0;
    }
}