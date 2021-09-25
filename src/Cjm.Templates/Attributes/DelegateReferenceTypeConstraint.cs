using System;
using System.Runtime.InteropServices;

namespace Cjm.Templates.Attributes
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct DelegateReferenceTypeConstraint : IEquatable<DelegateReferenceTypeConstraint>,
        IComparable<DelegateReferenceTypeConstraint>
    {
        public static readonly DelegateReferenceTypeConstraint AnyDelegateConstraint = new(null);

        public static DelegateReferenceTypeConstraint
            CreateSpecificDelegateTypeConstraint(Type delegateType) => new(delegateType);

        public Type? MustBeAssignableToDelegateOfType => _mustBeAssignableToDelegateOfType;

        public bool AnyDelegate => MustBeAssignableToDelegateOfType == null;

        

        private DelegateReferenceTypeConstraint(Type? delegateType) =>
            _mustBeAssignableToDelegateOfType = (delegateType)
                switch
                {
                    null => null,
                    {} dt when dt == typeof(Delegate) => null,
                    {} dt when typeof(Delegate).IsAssignableFrom(dt) => dt,
                    _ => throw new ArgumentException("Parameter must be a delegate type or null."),
                };

        public static bool operator ==(DelegateReferenceTypeConstraint lhs, DelegateReferenceTypeConstraint rhs) =>
            lhs._mustBeAssignableToDelegateOfType == rhs._mustBeAssignableToDelegateOfType;
        public static bool operator !=(DelegateReferenceTypeConstraint lhs, DelegateReferenceTypeConstraint rhs) =>
            !(lhs == rhs);
        public override int GetHashCode() => _mustBeAssignableToDelegateOfType?.GetHashCode() ?? int.MinValue;
        public override bool Equals(object? other) => other is DelegateReferenceTypeConstraint drtc && drtc == this;
        public bool Equals(DelegateReferenceTypeConstraint other) => other == this;
        public int CompareTo(DelegateReferenceTypeConstraint other) => Compare(this, other);
        public static int Compare(DelegateReferenceTypeConstraint lhs,DelegateReferenceTypeConstraint rhs)
        {
            return CompareTypes(lhs._mustBeAssignableToDelegateOfType, rhs._mustBeAssignableToDelegateOfType);
            static int CompareTypes(Type? l, Type? r)
            {
                if (ReferenceEquals(l, r)) return 0;
                if (l is null) return -1;
                if (r is null) return 1;

                return TheTypeComparer.Compare(l.AssemblyQualifiedName ?? (l.FullName ?? l.Name),
                    r.AssemblyQualifiedName ?? (r.FullName ?? r.Name));
            }
        }



        [FieldOffset(0)] private readonly Type? _mustBeAssignableToDelegateOfType;
        private static readonly StringComparer TheTypeComparer = StringComparer.Ordinal;
    }
}