using System;

namespace Cjm.Templates.Attributes
{
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
    public sealed class MustOverrideAttribute : Attribute, IEquatable<MustOverrideAttribute>
    {
        public MustOverrideAttributeTarget MustOverride { get; }
        public bool OverrideMustBeSealed { get; }

        public MustOverrideAttribute(MustOverrideAttributeTarget overrideTargets) 
            : this(overrideTargets, false) {}
        public MustOverrideAttribute(MustOverrideAttributeTarget overrideTargets, bool overrideMustBeSealed)
        {
            MustOverride = overrideTargets;
            OverrideMustBeSealed = overrideMustBeSealed;
        }

        public bool Equals(MustOverrideAttribute? other) => other?.MustOverride == MustOverride;
        public override bool Equals(object? other) => Equals(other as MustOverrideAttribute);
        /// <inheritdoc />
        public override int GetHashCode() => (byte)MustOverride;
        /// <inheritdoc />
        public override string ToString() =>
            $"[{nameof(MustOverrideAttribute)}] -- Required overrides: [{MustOverride}].";
        public static bool operator ==(MustOverrideAttribute? lhs, MustOverrideAttribute? rhs) =>
            ReferenceEquals(lhs, rhs) || lhs?.Equals(rhs) == true;
        public static bool operator !=(MustOverrideAttribute? lhs, MustOverrideAttribute? rhs) =>
            !(lhs == rhs);
    }

    [Flags]
    public enum MustOverrideAttributeTarget : byte
    {
        None = 0x00,
        ToString = 0x01,
        Equals = 0x02,
        GetHashCode = 0x04,
    }
}
