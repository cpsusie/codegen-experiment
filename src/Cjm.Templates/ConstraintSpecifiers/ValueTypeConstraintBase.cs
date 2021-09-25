using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace Cjm.Templates.ConstraintSpecifiers
{
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
}