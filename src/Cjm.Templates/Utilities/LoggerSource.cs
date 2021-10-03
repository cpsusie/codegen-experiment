using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Cjm.Templates.Utilities.SetOnce;
using LoggerLibrary;
[assembly: InternalsVisibleTo("Cjm.Templates.Test")]

namespace Cjm.Templates.Utilities
{
    internal static class LoggerSource
    {
        public static bool IsLoggerAlreadySet => TheLogger.IsSet;
        public static ICodeGenLogger Logger => TheLogger.Value;

        static LoggerSource()
        {
            TheLogger = new LocklessLazyWriteOnce<ICodeGenLogger>(CreateDefaultLogger);
        }

        public static void InjectAlternateLoggerOrThrow(ICodeGenLogger alternate)
        {
            try
            {
                TheLogger.SetNonDefaultValueOrThrow(alternate ?? throw new ArgumentNullException(nameof(alternate)),
                    true);
            }
            catch (LocklessFlagAlreadySetException)
            {
                if (!ReferenceEquals(alternate, Logger))
                {
                    throw;
                }
            }
            
        }
        

        static ICodeGenLogger CreateDefaultLogger()
        {
            CodeGenLogger.SupplyAlternateLoggingPathOrThrow(@"L:\Desktop\cjm_template_log.txt");
            return CodeGenLogger.Logger;
        }

        private static readonly LocklessLazyWriteOnce<ICodeGenLogger> TheLogger;
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
