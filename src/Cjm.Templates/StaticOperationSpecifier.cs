using System;
using System.Collections.Immutable;
using System.Linq;
using Cjm.Templates.SetOnce;

namespace Cjm.Templates
{
    public abstract class StaticOperationSpecifier : IEquatable<StaticOperationSpecifier>
    {
        public ParameterSpecifier ReturnType => _returnParameter;
        public Type OwningType => _owningType;
        public abstract ImmutableArray<ParameterSpecifier> InputParameterList { get; }
        public abstract string OperationName { get; }
        public abstract bool IsMethod { get; }
        public bool IsOperator => !IsMethod;
        protected Type ConcreteType => _concreteType.ConcreteType;
        protected string ConcreteTypeName => _concreteType.ConcreteTypeName;

        protected StaticOperationSpecifier(Type owningType, ParameterSpecifier returnParameter)
        {
            _owningType = owningType ?? throw new ArgumentNullException(nameof(owningType));
            _returnParameter = returnParameter;
            _concreteType = new LocklessConcreteType(this);
        }

        public bool Equals(StaticOperationSpecifier? other) =>
            ConcreteType == other?.ConcreteType && _returnParameter == other._returnParameter &&
            _owningType == other._owningType && InputParameterList.SequenceEqual(other.InputParameterList) &&
            IsImplEqualTo(other);
        public sealed override bool Equals(object? other) 
            => Equals(other as StaticOperationSpecifier);
        public static bool operator ==(StaticOperationSpecifier? lhs, StaticOperationSpecifier? rhs) =>
            ReferenceEquals(lhs, rhs) || lhs?.Equals(rhs) == true;
        public static bool operator !=(StaticOperationSpecifier? lhs, StaticOperationSpecifier? rhs) =>
            !(lhs == rhs);

        /// <inheritdoc />
        public sealed override string ToString() =>
            $"[{ConcreteTypeName}] -- OwningType: \t[{OwningType.Name}]; " +
            $"\t ReturnType: \t[{ReturnType}]; \tOperationName: \t[{OperationName}]; " +
            $"\tParameter Count: \t[{InputParameterList.Length}]; \t{GetImplStringRep()}";

        public sealed override int GetHashCode()
        {
            int hash = ConcreteType.GetHashCode();
            unchecked
            {
                hash = (hash * 397) ^ _returnParameter.GetHashCode();
                hash = (hash * 397) ^ _owningType.GetHashCode();
                hash = (hash * 397) ^ InputParameterList.Length;
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


        private readonly ParameterSpecifier _returnParameter;
        private readonly Type _owningType;
        private readonly LocklessConcreteType _concreteType;
    }
}