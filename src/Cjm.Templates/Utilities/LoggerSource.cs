using System;
using System.Diagnostics;
using LoggerLibrary;

namespace Cjm.Templates
{
    internal static class LoggerSource
    {
        public static readonly ICodeGenLogger Logger;

        static LoggerSource()
        {
            CodeGenLogger.SupplyAlternateLoggingPathOrThrow(@"L:\Desktop\cjm_template_log.txt");
            Logger =CodeGenLogger.Logger;
        }
    }

    internal static class DebugLog
    {
        [Conditional("DEBUG")]
        public static void LogMessage(string message) => LoggerSource.Logger.LogMessage(message);
        [Conditional("DEBUG")]
        public static void LogError(string error) => LoggerSource.Logger.LogError(error);
        [Conditional("DEBUG")]
        public static void LogException(Exception error) => LoggerSource.Logger.LogException(error);

        public static EntryExitLog CreateEel(string type, string method, string extraInfo) =>
            DoCreateEel(type, method, extraInfo);

        private static EntryExitLog DoCreateEel(string type, string method, string extraInfo)
        {
#if DEBUG
            return LoggerSource.Logger.CreateEel(type, method, extraInfo);
#else
            return default;
#endif
        }
    }

    internal static class TraceLog
    {
        [Conditional("TRACE")]
        public static void LogMessage(string message) => LoggerSource.Logger.LogMessage(message);

        public static void LogError(string error) => LoggerSource.Logger.LogError(error);

        public static void LogException(Exception error) => LoggerSource.Logger.LogException(error);

        public static EntryExitLog CreateEel(string type, string method, string extraInfo) =>
            DoCreateEel(type, method, extraInfo);

        private static EntryExitLog DoCreateEel(string type, string method, string extraInfo)
        {
#if TRACE
            return LoggerSource.Logger.CreateEel(type, method, extraInfo);
#else
            return default;
#endif
        }
    }
}
