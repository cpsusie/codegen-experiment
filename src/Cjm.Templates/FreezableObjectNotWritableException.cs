using System;
using Cjm.Templates.Utilities;

namespace Cjm.Templates
{
    public sealed class FreezableObjectNotWritableException : InvalidOperationException
    {
        public string FreezableObjectName { get; }

        public FreezeFlagCode CodeAtMomentOfAttemptedWrite { get; }

        public FreezableObjectNotWritableException(string freezableObjectName, string? badMemberName,
            FreezeFlagCode code, Exception? inner) : base(
            CreateMessage(freezableObjectName ?? throw new ArgumentNullException(nameof(freezableObjectName)),
                badMemberName, code, inner), inner)
        {
            FreezableObjectName = freezableObjectName;
            CodeAtMomentOfAttemptedWrite = code;
        }

        private static string CreateMessage(string freezableObjectName, string? badMemberName, FreezeFlagCode code, Exception? inner)
        {
            const string illegalCallMsgFrmt = "Illegal attempt to write to object {0}{1}";
            string memberNameText = !string.IsNullOrWhiteSpace(badMemberName) ? $"'s {badMemberName} member." : ".";
            return string.Format(illegalCallMsgFrmt, freezableObjectName, memberNameText) +
                   $" At moment of call freeze state was [{code}]." +
                   (inner != null ? " Consult inner exception for details." : string.Empty);
        }
    }
}
