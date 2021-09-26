using System;
using System.Collections.Immutable;
using System.Linq;
using Cjm.Templates.Utilities.SetOnce;

namespace Cjm.Templates.ConstraintSpecifiers
{
    public abstract class StaticOperationSpecifier : IEquatable<StaticOperationSpecifier>
    {
        public abstract Type OperationFormDelegate { get; }
        public abstract string OperationName { get; }
        public abstract bool IsMethod { get; }
        public bool IsOperator => !IsMethod;
        protected Type ConcreteType => _concreteType.ConcreteType;
        protected string ConcreteTypeName => _concreteType.ConcreteTypeName;

        protected StaticOperationSpecifier() => _concreteType = new LocklessConcreteType(this);

        public bool Equals(StaticOperationSpecifier? other) =>
            ConcreteType == other?.ConcreteType && OperationFormDelegate == other.OperationFormDelegate &&
            IsImplEqualTo(other);
        public sealed override bool Equals(object? other) 
            => Equals(other as StaticOperationSpecifier);
        public static bool operator ==(StaticOperationSpecifier? lhs, StaticOperationSpecifier? rhs) =>
            ReferenceEquals(lhs, rhs) || lhs?.Equals(rhs) == true;
        public static bool operator !=(StaticOperationSpecifier? lhs, StaticOperationSpecifier? rhs) =>
            !(lhs == rhs);

        /// <inheritdoc />
        public sealed override string ToString() =>
            $"[{ConcreteTypeName}] -- OperationName: \t[{OperationName}]; " +
            $"\tOperation Form: \t[{OperationFormDelegate.Name}]; \t{GetImplStringRep()}";

        public sealed override int GetHashCode()
        {
            int hash = ConcreteType.GetHashCode();
            unchecked
            {
                hash = (hash * 397) ^ OperationFormDelegate.GetHashCode();
                hash = (hash * 397) ^ GetImplHashCode();
            }
            return hash;
        }

        protected abstract bool IsImplEqualTo(StaticOperationSpecifier? other);
        protected abstract int GetImplHashCode();
        protected abstract string GetImplStringRep();

        protected static ImmutableArray<ParameterSpecifier> ExtractValuesFrom(ParameterSpecifier[] specifiers, string paramName)
        {
            var bldr = ImmutableArray.CreateBuilder<ParameterSpecifier>(specifiers.Length);
            for (int i = 0; i < specifiers.Length; ++i)
            {
                bldr.Add(ValueOrThrowIfVoid(in specifiers[i], paramName ?? nameof(specifiers)));
            }
            return bldr.Count == bldr.Capacity ? bldr.MoveToImmutable() : bldr.ToImmutable();
        }

        protected static ref readonly ParameterSpecifier ValueOrThrowIfVoid(in ParameterSpecifier ps, string paramName)
        {
            if (ps.ValidForInputParam)
            {
                return ref ps;
            }

            throw new ArgumentException($"One or more items in {paramName} contained an " +
                                        $"invalid value for an input parameter.  (First such value: {ps.ToString()}).",
                paramName);
        }


        
        private readonly LocklessConcreteType _concreteType;
    }
}