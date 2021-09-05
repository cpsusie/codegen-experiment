using Cjm.CodeGen.Attributes;
using LoggerLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Cjm.CodeGen
{
    internal static class LoggerSource
    {
        public static readonly ICodeGenLogger Logger = CodeGenLogger.Logger;
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

    [EnableAugmentedEnumerationExtensions(typeof(List<DateTime>))]  
    public static partial class ExtenderTemp 
    {

    } 
}