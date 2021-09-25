using System;
using System.Collections.Immutable;
using System.Linq;
using HpTimeStamps;

namespace Cjm.Templates.ConstraintSpecifiers
{
    public static class EnumConstraintTypeExtensions
    {
        public static ImmutableArray<EnumConstraintType> ValidConstraints => TheValidConstraintTypes;

        public static bool IsDefinedConstraint(this EnumConstraintType ect) => TheValidConstraintTypes.Contains(ect);

        public static EnumConstraintType ValueOrThrowIfNDef(this EnumConstraintType ect, string paramName) =>
            ect.IsDefinedConstraint()
                ? ect
                : throw new UndefinedEnumArgumentException<EnumConstraintType>(ect,
                    paramName ?? throw new ArgumentNullException(nameof(paramName)));

        public static bool ConstrainsToEnum(this EnumConstraintType ect) =>
            ect != EnumConstraintType.NoEnumConstraint && TheValidConstraintTypes.Contains(ect);

        private static readonly ImmutableArray<EnumConstraintType> TheValidConstraintTypes =
            Enum.GetValues(typeof(EnumConstraintType)).Cast<EnumConstraintType>().ToImmutableArray();
    }
}