using System;
using System.Runtime.InteropServices;

namespace Cjm.Templates.ConstraintSpecifiers
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct ValueTypeConstraintSpecifier : IEquatable<ValueTypeConstraintSpecifier>, IComparable<ValueTypeConstraintSpecifier>
    {
        #region Constraints For Structs
        public static readonly ValueTypeConstraintSpecifier PlainValueTypeConstraint =
            new(EnumConstraintType.NoEnumConstraint, ValueTypeConstraintCode.JustValueType);

        public static readonly ValueTypeConstraintSpecifier UnmanagedValueTypeConstraint =
            new(EnumConstraintType.NoEnumConstraint, ValueTypeConstraintCode.Unmanaged);

        public static readonly ValueTypeConstraintSpecifier ReadOnlyValueTypeConstraint = new(EnumConstraintType.NoEnumConstraint, ValueTypeConstraintCode.Readonly);

        public static readonly ValueTypeConstraintSpecifier StackOnlyValueTypeConstraint =
            new(EnumConstraintType.NoEnumConstraint, ValueTypeConstraintCode.StackOnly);

        public static readonly ValueTypeConstraintSpecifier UnmanagedReadOnlyValueTypeConstraint =
            new(EnumConstraintType.NoEnumConstraint, ValueTypeConstraintCode.Readonly | ValueTypeConstraintCode.Unmanaged);

        public static readonly ValueTypeConstraintSpecifier UnmanagedStackOnlyValueTypeConstraint =
            new(EnumConstraintType.NoEnumConstraint, ValueTypeConstraintCode.Unmanaged | ValueTypeConstraintCode.StackOnly);

        public static readonly ValueTypeConstraintSpecifier ReadOnlyStackOnlyValueTypeConstraint =
            new(EnumConstraintType.NoEnumConstraint, ValueTypeConstraintCode.Readonly | ValueTypeConstraintCode.StackOnly);

        public static readonly ValueTypeConstraintSpecifier UnmanagedReadOnlyStackOnlyValueTypeConstraint =
            new(EnumConstraintType.NoEnumConstraint, ValueTypeConstraintCode.Unmanaged | ValueTypeConstraintCode.Readonly |
                                                     ValueTypeConstraintCode.StackOnly); 
        #endregion

        #region Constraints for Enums
        public static readonly ValueTypeConstraintSpecifier AnyEnumValueTypeConstraint;
        public static readonly ValueTypeConstraintSpecifier AnySignedEnumValueTypeConstraint;
        public static readonly ValueTypeConstraintSpecifier AnyUnsignedEnumValueTypeConstraint;

        public static readonly ValueTypeConstraintSpecifier ByteBackedEnumValueTypeConstraint;
        public static readonly ValueTypeConstraintSpecifier UInt16BackedEnumValueTypeConstraint;
        public static readonly ValueTypeConstraintSpecifier UInt32BackedEnumValueTypeConstraint;
        public static readonly ValueTypeConstraintSpecifier UInt64BackedEnumValueTypeConstraint;

        public static readonly ValueTypeConstraintSpecifier SByteBackedEnumValueTypeConstraint;
        public static readonly ValueTypeConstraintSpecifier Int16BackedEnumValueTypeConstraint;
        public static readonly ValueTypeConstraintSpecifier Int32BackedEnumValueTypeConstraint;
        public static readonly ValueTypeConstraintSpecifier Int64BackedEnumValueTypeConstraint; 
        #endregion

        public bool ConstrainsToEnum => _enumConstraint.ConstrainsToEnum();
        public bool ConstrainsToStruct => !_enumConstraint.ConstrainsToEnum();
        public bool MustBeUnmanaged => _baseConstraint.MustBeUnmanaged; 
        public bool MustBeReadOnly => _baseConstraint.MustBeReadOnly; 
        public bool MustBeStackOnly => _baseConstraint.MustBeStackOnly;
        
        public EnumConstraintType EnumConstraint => _enumConstraint;

        public static implicit operator ValueTypeConstraintSpecifier(EnumConstraintType ect) =>
            new(ect, TheStructConstraintForEnums);

        public static implicit operator ValueTypeConstraintSpecifier(ValueTypeConstraintCode code) =>
            new(EnumConstraintType.NoEnumConstraint, code);

        private ValueTypeConstraintSpecifier(EnumConstraintType ecType, ValueTypeConstraintBase sct)
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
        public bool Equals(ValueTypeConstraintSpecifier other) => other == this;
        public override bool Equals(object? obj) => obj is ValueTypeConstraintSpecifier constraint && constraint == this;
        public static bool operator ==(ValueTypeConstraintSpecifier lhs, ValueTypeConstraintSpecifier rhs) => lhs._baseConstraint == rhs._baseConstraint && 
            lhs._enumConstraint == rhs._enumConstraint;
        public static bool operator !=(ValueTypeConstraintSpecifier lhs, ValueTypeConstraintSpecifier rhs) => !(lhs == rhs);
        public static bool operator >(ValueTypeConstraintSpecifier lhs, ValueTypeConstraintSpecifier rhs) => lhs.CompareTo(rhs) > 0;
        public static bool operator <(ValueTypeConstraintSpecifier lhs, ValueTypeConstraintSpecifier rhs) => lhs.CompareTo(rhs) < 0;
        public static bool operator >=(ValueTypeConstraintSpecifier lhs, ValueTypeConstraintSpecifier rhs) => !(lhs < rhs);
        public static bool operator <=(ValueTypeConstraintSpecifier lhs, ValueTypeConstraintSpecifier rhs) => !(lhs > rhs);

        public int CompareTo(ValueTypeConstraintSpecifier other)
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

        static ValueTypeConstraintSpecifier()
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
}