using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using HpTimeStamps;
using JetBrains.Annotations;

namespace Cjm.Templates.Attributes
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct ValueTypeConstraint : IEquatable<ValueTypeConstraint>, IComparable<ValueTypeConstraint>
    {
        #region Constraints For Structs
        public static readonly ValueTypeConstraint PlainValueTypeConstraint =
            new(EnumConstraintType.NoEnumConstraint, ValueTypeConstraintCode.JustValueType);

        public static readonly ValueTypeConstraint UnmanagedValueTypeConstraint =
            new(EnumConstraintType.NoEnumConstraint, ValueTypeConstraintCode.Unmanaged);

        public static readonly ValueTypeConstraint ReadOnlyValueTypeConstraint = new(EnumConstraintType.NoEnumConstraint, ValueTypeConstraintCode.Readonly);

        public static readonly ValueTypeConstraint StackOnlyValueTypeConstraint =
            new(EnumConstraintType.NoEnumConstraint, ValueTypeConstraintCode.StackOnly);

        public static readonly ValueTypeConstraint UnmanagedReadOnlyValueTypeConstraint =
            new(EnumConstraintType.NoEnumConstraint, ValueTypeConstraintCode.Readonly | ValueTypeConstraintCode.Unmanaged);

        public static readonly ValueTypeConstraint UnmanagedStackOnlyValueTypeConstraint =
            new(EnumConstraintType.NoEnumConstraint, ValueTypeConstraintCode.Unmanaged | ValueTypeConstraintCode.StackOnly);

        public static readonly ValueTypeConstraint ReadOnlyStackOnlyValueTypeConstraint =
            new(EnumConstraintType.NoEnumConstraint, ValueTypeConstraintCode.Readonly | ValueTypeConstraintCode.StackOnly);

        public static readonly ValueTypeConstraint UnmanagedReadOnlyStackOnlyValueTypeConstraint =
            new(EnumConstraintType.NoEnumConstraint, ValueTypeConstraintCode.Unmanaged | ValueTypeConstraintCode.Readonly |
                                                     ValueTypeConstraintCode.StackOnly); 
        #endregion

        #region Constraints for Enums
        public static readonly ValueTypeConstraint AnyEnumValueTypeConstraint;
        public static readonly ValueTypeConstraint AnySignedEnumValueTypeConstraint;
        public static readonly ValueTypeConstraint AnyUnsignedEnumValueTypeConstraint;

        public static readonly ValueTypeConstraint ByteBackedEnumValueTypeConstraint;
        public static readonly ValueTypeConstraint UInt16BackedEnumValueTypeConstraint;
        public static readonly ValueTypeConstraint UInt32BackedEnumValueTypeConstraint;
        public static readonly ValueTypeConstraint UInt64BackedEnumValueTypeConstraint;

        public static readonly ValueTypeConstraint SByteBackedEnumValueTypeConstraint;
        public static readonly ValueTypeConstraint Int16BackedEnumValueTypeConstraint;
        public static readonly ValueTypeConstraint Int32BackedEnumValueTypeConstraint;
        public static readonly ValueTypeConstraint Int64BackedEnumValueTypeConstraint; 
        #endregion

        public bool ConstrainsToEnum => _enumConstraint.ConstrainsToEnum();
        public bool ConstrainsToStruct => !_enumConstraint.ConstrainsToEnum();
        public bool MustBeUnmanaged => _baseConstraint.MustBeUnmanaged; 
        public bool MustBeReadOnly => _baseConstraint.MustBeReadOnly; 
        public bool MustBeStackOnly => _baseConstraint.MustBeStackOnly;
        
        public EnumConstraintType EnumConstraint => _enumConstraint;

        public static implicit operator ValueTypeConstraint(EnumConstraintType ect) =>
            new(ect, TheStructConstraintForEnums);

        public static implicit operator ValueTypeConstraint(ValueTypeConstraintCode code) =>
            new(EnumConstraintType.NoEnumConstraint, code);

        private ValueTypeConstraint(EnumConstraintType ecType, ValueTypeConstraintBase sct)
        {
            _enumConstraint = ecType.ValueOrThrowIfNDef(nameof(ecType));
            _baseConstraint = (_enumConstraint.ConstrainsToEnum() && sct != TheStructConstraintForEnums)
                ? throw new ArgumentException(
                    $"If the enum constraint is specified, parameter must equal {TheStructConstraintForEnums}.  Its actual value was: {sct.ToString()}.",
                    nameof(sct))
                : sct;
        }

        /// <inheritdoc />
        public override string ToString() => ConstrainsToEnum ? _enumConstraint.ToString() : _baseConstraint.ToString();
        public bool Equals(ValueTypeConstraint other) => other == this;
        public override bool Equals(object? obj) => obj is ValueTypeConstraint constraint && constraint == this;
        public static bool operator ==(ValueTypeConstraint lhs, ValueTypeConstraint rhs) => lhs._baseConstraint == rhs._baseConstraint && 
            lhs._enumConstraint == rhs._enumConstraint;
        public static bool operator !=(ValueTypeConstraint lhs, ValueTypeConstraint rhs) => !(lhs == rhs);
        public static bool operator >(ValueTypeConstraint lhs, ValueTypeConstraint rhs) => lhs.CompareTo(rhs) > 0;
        public static bool operator <(ValueTypeConstraint lhs, ValueTypeConstraint rhs) => lhs.CompareTo(rhs) < 0;
        public static bool operator >=(ValueTypeConstraint lhs, ValueTypeConstraint rhs) => !(lhs < rhs);
        public static bool operator <=(ValueTypeConstraint lhs, ValueTypeConstraint rhs) => !(lhs > rhs);

        public int CompareTo(ValueTypeConstraint other)
        {
            int strctComp = _baseConstraint.CompareTo(other._baseConstraint);
            return strctComp == 0 ? Compare(_enumConstraint, other._enumConstraint) : strctComp;

            static int Compare(EnumConstraintType l, EnumConstraintType r) => l == r ? 0 : (l > r ? 1 : -1);
        }

        public override int GetHashCode()
        {
            int hash = (int)_enumConstraint;
            unchecked
            {
                hash = (hash * 397) ^ _baseConstraint.GetHashCode();
            }
            return hash;
        }

        static ValueTypeConstraint()
        {
            TheStructConstraintForEnums = ValueTypeConstraintCode.Unmanaged;
            
            AnyEnumValueTypeConstraint = new(EnumConstraintType.AnyConcreteEnum, TheStructConstraintForEnums);
            AnyUnsignedEnumValueTypeConstraint = new(EnumConstraintType.AnyUnsigned, TheStructConstraintForEnums);
            AnySignedEnumValueTypeConstraint = new(EnumConstraintType.AnySigned, TheStructConstraintForEnums);

            ByteBackedEnumValueTypeConstraint = new(EnumConstraintType.Byte, TheStructConstraintForEnums);
            UInt16BackedEnumValueTypeConstraint = new(EnumConstraintType.UInt16, TheStructConstraintForEnums);
            UInt32BackedEnumValueTypeConstraint = new(EnumConstraintType.UInt32, TheStructConstraintForEnums);
            UInt64BackedEnumValueTypeConstraint = new(EnumConstraintType.UInt64, TheStructConstraintForEnums);

            SByteBackedEnumValueTypeConstraint = new(EnumConstraintType.SByte, TheStructConstraintForEnums);
            Int16BackedEnumValueTypeConstraint = new(EnumConstraintType.Int16, TheStructConstraintForEnums);
            Int32BackedEnumValueTypeConstraint = new(EnumConstraintType.Int32, TheStructConstraintForEnums);
            Int64BackedEnumValueTypeConstraint = new(EnumConstraintType.Int64, TheStructConstraintForEnums);

        }


        [FieldOffset(0)] private readonly ValueTypeConstraintBase _baseConstraint;
        [FieldOffset(1)] private readonly EnumConstraintType _enumConstraint;
        private static readonly ValueTypeConstraintBase TheStructConstraintForEnums;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal readonly struct ValueTypeConstraintBase : IEquatable<ValueTypeConstraintBase>, IComparable<ValueTypeConstraintBase>
    {
        public static implicit operator ValueTypeConstraintBase(ValueTypeConstraintCode code) => new(code);
        public static implicit operator ValueTypeConstraintCode(ValueTypeConstraintBase val) => val._code;

        public bool MustBeUnmanaged => (_code & ValueTypeConstraintCode.Unmanaged) == ValueTypeConstraintCode.Unmanaged;
        public bool MustBeReadOnly => (_code & ValueTypeConstraintCode.Readonly) == ValueTypeConstraintCode.Readonly;
        public bool MustBeStackOnly => (_code & ValueTypeConstraintCode.StackOnly) == ValueTypeConstraintCode.StackOnly;

        public int CompareTo(ValueTypeConstraintBase other) => other == this ? 0 : ((this > other) ? 1 : 0);
        public bool Equals(ValueTypeConstraintBase other) => other == this;
        public override bool Equals(object? obj) => obj is ValueTypeConstraintBase constraint && constraint == this;
        public override int GetHashCode() => (int)_code;
        public static bool operator ==(ValueTypeConstraintBase lhs, ValueTypeConstraintBase rhs) => lhs._code == rhs._code;
        public static bool operator !=(ValueTypeConstraintBase lhs, ValueTypeConstraintBase rhs) => !(lhs == rhs);
        public static bool operator >(ValueTypeConstraintBase lhs, ValueTypeConstraintBase rhs) => lhs._code > rhs._code;
        public static bool operator <(ValueTypeConstraintBase lhs, ValueTypeConstraintBase rhs) => lhs._code < rhs._code;
        public static bool operator >=(ValueTypeConstraintBase lhs, ValueTypeConstraintBase rhs) => lhs._code >= rhs._code;
        public static bool operator <=(ValueTypeConstraintBase lhs, ValueTypeConstraintBase rhs) => lhs._code <= rhs._code;
        [Pure] public ValueTypeConstraintBase WithUnmanagedConstraint() => new(_code | ValueTypeConstraintCode.Unmanaged);
        [Pure] public ValueTypeConstraintBase WithoutUnmanagedConstraint() => new(_code & (~ValueTypeConstraintCode.Unmanaged));
        [Pure] public ValueTypeConstraintBase WithReadOnlyConstraint() => new(_code | ValueTypeConstraintCode.Readonly);
        [Pure] public ValueTypeConstraintBase WithoutReadOnlyConstraint() => new(_code & (~ValueTypeConstraintCode.Readonly));
        [Pure] public ValueTypeConstraintBase WithStackOnlyConstraint() => new(_code | ValueTypeConstraintCode.StackOnly);
        [Pure] public ValueTypeConstraintBase WithoutStackOnlyConstraint() => new(_code & (~ValueTypeConstraintCode.StackOnly));
        /// <inheritdoc />
        public override string ToString() => _code.ToString();

        private ValueTypeConstraintBase(ValueTypeConstraintCode code) => _code = code;


        [FieldOffset(0)] private readonly ValueTypeConstraintCode _code;
    }

    [Flags]
    public enum ValueTypeConstraintCode : byte //ushort
    {
        JustValueType = 0x00,
        Unmanaged = 0x01,
        Readonly = 0x02,
        StackOnly = 0x04,
    }

    public enum EnumConstraintType : byte
    {
        NoEnumConstraint,
        AnyConcreteEnum,
        Byte,
        SByte,
        UInt16,
        Int16,
        UInt32,
        Int32,
        UInt64,
        Int64,
        AnyUnsigned,
        AnySigned
    }

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