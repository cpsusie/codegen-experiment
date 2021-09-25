using System;
using System.Collections.Immutable;
using System.Linq;
using HpTimeStamps;

namespace TemplateLibraryTests
{
    public readonly struct ParameterSpecifier : IEquatable<ParameterSpecifier>
    {
        public Type InputParameterType => _inputParameterType ?? typeof(void);
        public NullabilitySpecifier ParameterNullability { get; }
        public PassBySpecifier PassBySpecifier { get; }

        public ParameterSpecifier(Type theType) : this(theType, NullabilitySpecifier.Unknown,
            PassBySpecifier.ByValue) { }
        public ParameterSpecifier(Type theType, NullabilitySpecifier spec) : this(theType, spec,
            PassBySpecifier.ByValue) { }
        public ParameterSpecifier(Type theType, PassBySpecifier spec) : this(theType, NullabilitySpecifier.Unknown,
            spec) { }

        private ParameterSpecifier(Type theType, NullabilitySpecifier nullSpecInfo, PassBySpecifier specifier)
        {
            _inputParameterType = theType ?? throw new ArgumentNullException(nameof(theType));
            ParameterNullability = nullSpecInfo.ValueOrDefaultIfNDef(nameof(nullSpecInfo));
            PassBySpecifier = specifier.ValueOrDefaultIfNDef(nameof(specifier));
        }

        public override int GetHashCode()
        {
            int hash = InputParameterType.GetHashCode();
            unchecked
            {
                hash = (hash * 397) ^ ((byte)ParameterNullability);
                hash = (hash * 397) ^ ((byte)PassBySpecifier);
            }
            return hash;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is ParameterSpecifier s && s == this;
        public static bool operator ==(in ParameterSpecifier lhs, in ParameterSpecifier rhs) =>
            lhs.InputParameterType == rhs.InputParameterType && lhs.ParameterNullability == rhs.ParameterNullability &&
            lhs.PassBySpecifier == rhs.PassBySpecifier;
        public static bool operator !=(in ParameterSpecifier lhs, in ParameterSpecifier rhs) => !(lhs == rhs);
        public bool Equals(ParameterSpecifier other) => other == this;
        /// <inheritdoc />
        public override string ToString() =>
            $"[{nameof(InputParameterType)}] -- Type: [{InputParameterType.Name}]; " +
            $"Nullability: [{ParameterNullability}]; PassBy: [{PassBySpecifier}].";



        private readonly Type? _inputParameterType;
    }

    public enum PassBySpecifier : byte
    {
        ByValue = 0,
        ByValueOrByInRef,
        ByRef,
        ByOutRef,
        ByInRef
    }

    public enum NullabilitySpecifier : byte
    {
        Unknown = 0,
        NotNull,
        CanBeNull
    }

    public static class PassBySpecifierExtensions
    {
        public static readonly ImmutableArray<PassBySpecifier> DefinedValues;

        public static bool IsDefined(this PassBySpecifier pbs) => DefinedValues.Contains(pbs);

        public static PassBySpecifier ValueOrDefaultIfNDef(this PassBySpecifier pbs, string? paramName) =>
            pbs.IsDefined()
                ? pbs
                : throw new UndefinedEnumArgumentException<PassBySpecifier>(pbs, paramName ?? nameof(pbs));

        public static int CompareTo(this PassBySpecifier pbs, PassBySpecifier other) =>
            ((byte)pbs).CompareTo((byte)other);

        public static int GetHashCode(PassBySpecifier pbs) => (byte)pbs;

        static PassBySpecifierExtensions() => DefinedValues =
            Enum.GetValues(typeof(PassBySpecifier)).Cast<PassBySpecifier>().ToImmutableArray();
    }

    public static class NullabilitySpecifierExtensions
    {
        public static readonly ImmutableArray<NullabilitySpecifier> DefinedValues;

        public static bool IsDefined(this NullabilitySpecifier pbs) => DefinedValues.Contains(pbs);

        public static NullabilitySpecifier ValueOrDefaultIfNDef(this NullabilitySpecifier pbs, string? paramName) =>
            pbs.IsDefined()
                ? pbs
                : throw new UndefinedEnumArgumentException<NullabilitySpecifier>(pbs, paramName ?? nameof(pbs));

        public static int CompareTo(this NullabilitySpecifier pbs, NullabilitySpecifier other) =>
            ((byte)pbs).CompareTo((byte)other);

        public static int GetHashCode(NullabilitySpecifier pbs) => (byte)pbs;

        static NullabilitySpecifierExtensions() => DefinedValues =
            Enum.GetValues(typeof(NullabilitySpecifier)).Cast<NullabilitySpecifier>().ToImmutableArray();
    }
}
