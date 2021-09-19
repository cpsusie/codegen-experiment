using System;
using System.Collections.Generic;
using System.Text;
using Cjm.Templates.SetOnce;

namespace Cjm.Templates.Attributes
{
    public abstract class ConstraintAttribute : Attribute, IEquatable<ConstraintAttribute>
    {
        protected Type ConcreteType => _concreteType.ConcreteType;
        protected string ConcreteTypeName => _concreteType.ConcreteTypeName;

        private protected ConstraintAttribute() => _concreteType = new LocklessConcreteType(this);

        public bool Equals(ConstraintAttribute? other) => ConcreteType.Equals(other?.ConcreteType) && IsEqualTo(other);
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
            int hash = ConcreteType.GetHashCode();
            unchecked
            {
                hash = (hash * 397) ^ CalculateHashCode();
            }
            return hash;
        }

        private readonly LocklessConcreteType _concreteType;
    }
}
