using System;
using System.Collections.Immutable;
using System.Linq;
using HpTimeStamps;

namespace Cjm.Templates
{
    public enum ExtractionResult : byte
    {
        ErrorUnknown = 0,
        NotApplicable,
        InterfaceWithErrors,
        ImplementationWithErrors,
        InstantiationWithErrors,
        InterfaceOk,
        ImplementationOk,
        InstantiationOk,
    }

    public static class ExtractionResultExtensions
    {
        public static readonly ImmutableArray<ExtractionResult> AllDefinedResults;

        public static bool IsDefined(this ExtractionResult result) => AllDefinedResults.Contains(result);

        public static ExtractionResult ValueOrThrowIfNDef(this ExtractionResult result, string? argName) =>
            result.IsDefined()
                ? result
                : throw new UndefinedEnumArgumentException<ExtractionResult>(result, argName ?? nameof(result));
        public static ExtractionResult ValueOrDefaultIfNDef(this ExtractionResult result) =>
            result.IsDefined() ? result : ExtractionResult.ErrorUnknown;

        public static bool IsSuccessResult(this ExtractionResult result) => result == ExtractionResult.InterfaceOk ||
                                                                            result == ExtractionResult
                                                                                .ImplementationOk ||
                                                                            result == ExtractionResult.InstantiationOk;

        public static bool IsNullResult(this ExtractionResult result) => result == ExtractionResult.NotApplicable;

        public static bool IsErrorResult(this ExtractionResult result) =>
            !result.IsSuccessResult() && !result.IsNullResult();

        public static bool IsInterfaceSpecific(this ExtractionResult result) =>
            result == ExtractionResult.InterfaceOk || result == ExtractionResult.InterfaceWithErrors;
        public static bool IsImplementationSpecific(this ExtractionResult result) =>
            result == ExtractionResult.ImplementationOk || result == ExtractionResult.ImplementationWithErrors;

        public static bool IsInstantiationSpecific(this ExtractionResult result) =>
            result == ExtractionResult.InstantiationWithErrors || result == ExtractionResult.InstantiationOk;



        static ExtractionResultExtensions() => AllDefinedResults =
            Enum.GetValues(typeof(ExtractionResult)).Cast<ExtractionResult>().ToImmutableArray();
    }
}