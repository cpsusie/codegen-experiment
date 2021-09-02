using System;
using System.Collections.Generic;
using System.Text;
using Cjm.CodeGen.Attributes;
using HpTimeStamps;
using LoggerLibrary;
using MonotonicContext = HpTimeStamps.MonotonicStampContext;
namespace Cjm.CodeGen.Exceptions
{
    using MonotonicStamp = HpTimeStamps.MonotonicTimeStamp<MonotonicContext>;
    using MonoStampSource = TimeStampProvider;
    public abstract class CodeGenerationException : ApplicationException
    {
        public ref readonly PortableMonotonicStamp TimeStamp => ref _timeStamp;

        protected CodeGenerationException(string message, Exception? inner) : this(message, inner, MonoStampSource.MonoNow) {}

        protected CodeGenerationException(string message, Exception? inner, MonotonicStamp stamp) : base(CreateMessage(
            message ?? throw new ArgumentNullException(nameof(message)), inner, stamp), inner) =>
            _timeStamp = (PortableMonotonicStamp)stamp;

        private static string CreateMessage(string message, Exception? inner, MonotonicStamp stamp) =>
            string.Format(GeneralMessageFormatStr, stamp.ToUtcDateTime().ToString("O")) + "\"" + message +
            "\"." + (inner != null ? "  Consult inner exception for details." : string.Empty);

        protected const string GeneralMessageFormatStr = "At [{0}] the code generator utility encountered an error: ";
        private readonly PortableMonotonicStamp _timeStamp;
    }

    public sealed class CannotFindAttributeSymbolException : CodeGenerationException
    {
        public Type UnfindableAttributeSymbol { get; }

        internal CannotFindAttributeSymbolException(Type attributeCannotFind, Exception? inner) : base(
            string.Format(ErrMsgFrmtStr,
                (attributeCannotFind ?? throw new ArgumentNullException(nameof(attributeCannotFind))).FullName),
            inner) => UnfindableAttributeSymbol = attributeCannotFind;

        internal CannotFindAttributeSymbolException(Type attributeCannotFind, string metadataNameUsedToSearch, Exception? inner) : base(
            string.Format(ErrMsgWithMetaDataNameFrmtStr,
                (attributeCannotFind ?? throw new ArgumentNullException(nameof(attributeCannotFind))).FullName, metadataNameUsedToSearch ?? throw new ArgumentNullException(nameof(metadataNameUsedToSearch))),
            inner) => UnfindableAttributeSymbol = attributeCannotFind;

        internal CannotFindAttributeSymbolException(Type attributeCannotFind) : base(
            string.Format(ErrMsgFrmtStr,
                (attributeCannotFind ?? throw new ArgumentNullException(nameof(attributeCannotFind))).FullName),
            null) => UnfindableAttributeSymbol = attributeCannotFind;

        internal CannotFindAttributeSymbolException(Type attributeCannotFind, string metadataNameUsedToSearch) : base(
            string.Format(ErrMsgWithMetaDataNameFrmtStr,
                (attributeCannotFind ?? throw new ArgumentNullException(nameof(attributeCannotFind))).FullName, metadataNameUsedToSearch ?? throw new ArgumentNullException(nameof(metadataNameUsedToSearch))),
            null) => UnfindableAttributeSymbol = attributeCannotFind;

        private const string ErrMsgFrmtStr = "it could not find the {0} attribute.";
        private const string ErrMsgWithMetaDataNameFrmtStr =
            "it could not find the {0} attribute via metadata name {1}.";
    }
}
