using System;
using Cjm.Templates.Utilities.SetOnce;

namespace Cjm.Templates.Attributes
{
    
    public abstract class ConstraintAttribute : Attribute, IEquatable<ConstraintAttribute>
    {
        public const string ShortName = "Constraint";
        public Type ConcreteConstraintType => _concreteType.ConcreteType;
        protected string ConcreteTypeName => _concreteType.ConcreteTypeName;

        private protected ConstraintAttribute() => _concreteType = new LocklessConcreteType(this);

        public bool Equals(ConstraintAttribute? other) => ConcreteConstraintType == other?.ConcreteConstraintType && IsEqualTo(other);
        public sealed override bool Equals(object? obj) => Equals(obj as ConstraintAttribute);
        protected abstract string GetImplStringRep();
        protected abstract bool IsEqualTo(ConstraintAttribute? other);
        protected abstract int CalculateHashCode();
        public static bool operator ==(ConstraintAttribute? lhs, ConstraintAttribute? rhs) =>
            ReferenceEquals(lhs, rhs) || lhs?.Equals(rhs) == true;
        public static bool operator !=(ConstraintAttribute? lhs, ConstraintAttribute? rhs) =>
            !(lhs == rhs);
        /// <inheritdoc />
        public override string ToString() => $"[{ConcreteTypeName}] -- {GetImplStringRep()}";
        
        public sealed override int GetHashCode()
        {
            int hash = ConcreteConstraintType.GetHashCode();
            unchecked
            {
                hash = (hash * 397) ^ CalculateHashCode();
            }
            return hash;
        }

        private readonly LocklessConcreteType _concreteType;
    }
}
