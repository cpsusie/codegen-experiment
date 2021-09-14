using System;

namespace Cjm.CodeGen
{
    public sealed class SemanticData : IEquatable<SemanticData>
    {
        public ref readonly EnableAugmentedEnumerationTargetTypeData TargetTypeData => ref _targetTypeData;
        public ref readonly AttribTargetData AttributeTargetData => ref _attribInfo;

        internal SemanticData(in EnableAugmentedEnumerationTargetTypeData ttd, in AttribTargetData atd)
        {
            _attribInfo = atd;
            _targetTypeData = ttd;
            _stringRep = new LocklessLazyWriteOnce<string>(GetStringRep);
        }

        public bool Equals(SemanticData? other) =>
            _attribInfo == other?._attribInfo && _targetTypeData == other._targetTypeData;

        public override int GetHashCode()
        {
            int hash = _attribInfo.GetHashCode();
            unchecked
            {
                hash = (hash * 397) ^ _targetTypeData.GetHashCode();
            }

            return hash;
        }

        public override bool Equals(object? other) => Equals(other as SemanticData);

        public static bool operator ==(SemanticData? lhs, SemanticData? rhs) =>
            ReferenceEquals(lhs, rhs) || lhs?.Equals(rhs) == true;

        public static bool operator !=(SemanticData? lhs, SemanticData? rhs) => !(lhs == rhs);

        public override string ToString() => _stringRep.Value;

        private string GetStringRep()
        {
            const string semDat = nameof(SemanticData);
            const string formatStr = "{0} -- Attribute Target Data: \"{1}\"; Target Type Data: \"{2}\".";
            string attribDat = _attribInfo.ToString();
            string targTypeData = _targetTypeData.ToString();
            return string.Format(formatStr, semDat, attribDat, targTypeData);
        }

        private readonly EnableAugmentedEnumerationTargetTypeData _targetTypeData;
        private readonly AttribTargetData _attribInfo;
        private readonly LocklessLazyWriteOnce<string> _stringRep;
    }
}