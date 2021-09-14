using System;

namespace Cjm.CodeGen
{
    public sealed class UsableSemanticData : IEquatable<UsableSemanticData?>
    {
        
        public SemanticData SemanticInfo => _semanticData;
        public ref readonly GenerationData GenerationInfo => ref _generationData;

        private UsableSemanticData(SemanticData sd, in GenerationData gd)
        {
            _generationData = gd.IsInvalidDefault
                ? throw new ArgumentException("Parameter was invalid, uninitialized default value.", nameof(gd))
                : gd;
            _semanticData = sd ?? throw new ArgumentNullException(nameof(sd));
            _stringRep = new LocklessLazyWriteOnce<string>(GetStringRep);
        }

        public bool Equals(UsableSemanticData? other) =>
            other?._semanticData == _semanticData && other._generationData == _generationData;

        public override int GetHashCode()
        {
            int hash = _semanticData.GetHashCode();
            unchecked
            {
                hash = (hash * 397) ^ _generationData.GetHashCode();
            }
            return hash;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => Equals(obj as UsableSemanticData);
        public static bool operator ==(UsableSemanticData lhs, UsableSemanticData rhs) =>
            ReferenceEquals(lhs, rhs) || lhs?.Equals(rhs) == true;
        public static bool operator !=(UsableSemanticData lhs, UsableSemanticData rhs) => !(lhs == rhs);

        /// <inheritdoc />
        public override string ToString() => _stringRep.Value;
           
        private string GetStringRep() =>
            $"{nameof(UsableSemanticData)} -- Semantic Data: {_semanticData.ToString()} \tGenerationData: {_generationData.ToString()}";
        private readonly LocklessLazyWriteOnce<string> _stringRep;
        private readonly GenerationData _generationData;
        private readonly SemanticData _semanticData;
    }
}