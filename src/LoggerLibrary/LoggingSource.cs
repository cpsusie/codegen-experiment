using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using HpTimeStamps;

namespace LoggerLibrary
{
    [DataContract]
    public readonly struct ProcessLoggingContext : IEquatable<ProcessLoggingContext>, IComparable<ProcessLoggingContext>
    {
        #region Factory Method
        public static ProcessLoggingContext CreateContext()
        {
            var now = TimeStampProvider.MonoNow;
            var stamp = (PortableMonotonicStamp)now;
            var wallClockStamp = TimeStampProvider.WallStamp;
            var guid = Guid.NewGuid();
            var process = Process.GetCurrentProcess();
            return new ProcessLoggingContext(in stamp, wallClockStamp, guid, process.Id, process.ProcessName);
        }
        #endregion

        #region Static Values
        public static ref readonly ProcessLoggingContext InvalidDefault => ref TheInvalidContext;
        #endregion

        #region Public Properties
        [DataMember]
        public int ProcessId { get; }
        public Guid ProcessInstanceId => _processInstanceId;
        public PortableMonotonicStamp FirstLoggedTimeInProcess => _firstLoggedTimeInProcess;
        [DataMember]
        public DateTime FirstLoggedWallClockTime { get; }
        public string ProcessName => _processName ?? string.Empty; 
        #endregion

        #region Private CTOR
        private ProcessLoggingContext(in PortableMonotonicStamp firstLoggedAt, DateTime wallClock, Guid id,
            int processId, string processName)
        {
            _firstLoggedTimeInProcess = firstLoggedAt;
            FirstLoggedWallClockTime = wallClock;
            _processInstanceId = id;
            _processName = processName ?? throw new ArgumentNullException(nameof(processName));
            ProcessId = processId;
        }
        #endregion

        #region Public Methods and Operators
        public static int Compare(in ProcessLoggingContext lhs, in ProcessLoggingContext rhs)
        {
            int ret;
            int firstLoggedComp =
                PortableMonotonicStamp.Compare(in lhs._firstLoggedTimeInProcess, in rhs._firstLoggedTimeInProcess);
            if (firstLoggedComp == 0)
            {
                int guidComp = lhs._processInstanceId.CompareTo(rhs._processInstanceId);
                if (guidComp == 0)
                {
                    int procIdComp = lhs.ProcessId.CompareTo(rhs.ProcessId);
                    if (procIdComp == 0)
                    {
                        int wallClockComp = lhs.FirstLoggedWallClockTime.CompareTo(rhs.FirstLoggedWallClockTime);
                        ret = wallClockComp == 0
                            ? TheComparer.Compare(lhs.ProcessName, rhs.ProcessName)
                            : wallClockComp;
                    }
                    else
                    {
                        ret = procIdComp;
                    }
                }
                else
                {
                    ret = guidComp;
                }
            }
            else
            {
                ret = firstLoggedComp;
            }
            return ret;
        }

        public override int GetHashCode()
        {
            int hash = _firstLoggedTimeInProcess.GetHashCode();
            unchecked
            {
                hash = (hash * 397) ^ _processInstanceId.GetHashCode();
            }
            return hash;
        }

        public static bool operator ==(in ProcessLoggingContext lhs, in ProcessLoggingContext rhs)
            => lhs._processInstanceId == rhs._processInstanceId &&
               lhs._firstLoggedTimeInProcess == rhs._firstLoggedTimeInProcess && lhs.ProcessId == rhs.ProcessId &&
               lhs.FirstLoggedWallClockTime == rhs.FirstLoggedWallClockTime &&
               TheComparer.Equals(lhs.ProcessName, rhs.ProcessName);
        public static bool operator !=(in ProcessLoggingContext lhs, in ProcessLoggingContext rhs) => !(lhs == rhs);
        public static bool operator >(in ProcessLoggingContext lhs, in ProcessLoggingContext rhs)
            => Compare(in lhs, in rhs) > 0;
        public static bool operator <(in ProcessLoggingContext lhs, in ProcessLoggingContext rhs) =>
            Compare(in lhs, in rhs) < 0;
        public static bool operator >=(in ProcessLoggingContext lhs, in ProcessLoggingContext rhs) => !(lhs < rhs);
        public static bool operator <=(in ProcessLoggingContext lhs, in ProcessLoggingContext rhs) => !(lhs > rhs);
        public override bool Equals(object? other) => other is ProcessLoggingContext plc && plc == this;
        public override string ToString() => $"Process Id: [{ProcessId}], Process Name: [{ProcessName}]";
        public bool Equals(ProcessLoggingContext other) => other == this;
        public int CompareTo(ProcessLoggingContext other) => Compare(in this, in other);
        #endregion

        #region Private Data
        [DataMember] private readonly PortableMonotonicStamp _firstLoggedTimeInProcess;
        [DataMember] private readonly Guid _processInstanceId;
        [DataMember] private readonly string? _processName;
        private static readonly TrimmedStringComparer TheComparer = TrimmedStringComparer.TrimmedOrdinal;
        private static readonly ProcessLoggingContext TheInvalidContext = default; 
        #endregion
    }
    
    [DataContract]
    public readonly struct ThreadLoggingContext : IEquatable<ThreadLoggingContext>, IComparable<ThreadLoggingContext>
    {
        internal static ThreadLoggingContext Create() =>
            new ThreadLoggingContext(Thread.CurrentThread.Name ?? GetNoNameThreadId(),
                Thread.CurrentThread.ManagedThreadId);

        public string ThreadName => _threadName ?? string.Empty;
        [DataMember] public int ManagedThreadId { get; }

        private ThreadLoggingContext(string threadName, int managedThreadId)
        {
            _threadName = threadName ?? throw new ArgumentNullException(nameof(threadName));
            ManagedThreadId = managedThreadId;
        }

        public static int Compare(in ThreadLoggingContext lhs, in ThreadLoggingContext rhs)
        {
            int compRet = lhs.ManagedThreadId.CompareTo(rhs.ManagedThreadId);
            return compRet == 0 ? TheComparer.Compare(lhs.ThreadName, rhs.ThreadName) : compRet;
        }

        public override int GetHashCode()
        {
            int hash = ManagedThreadId;
            unchecked
            {
                hash = (hash * 397) ^ TheComparer.GetHashCode(ThreadName);
            }
            return hash;
        }

        public static bool operator ==(in ThreadLoggingContext lhs, in ThreadLoggingContext rhs) =>
            lhs.ManagedThreadId == rhs.ManagedThreadId && TheComparer.Equals(lhs.ThreadName, rhs.ThreadName);
        public static bool operator !=(in ThreadLoggingContext lhs, in ThreadLoggingContext rhs) => !(lhs == rhs);
        public static bool operator >(in ThreadLoggingContext lhs, in ThreadLoggingContext rhs) =>
            Compare(in lhs, in rhs) > 0;
        public static bool operator <(in ThreadLoggingContext lhs, in ThreadLoggingContext rhs) =>
            Compare(in lhs, in rhs) < 0;
        public static bool operator >=(in ThreadLoggingContext lhs, in ThreadLoggingContext rhs) => !(lhs < rhs);
        public static bool operator <=(in ThreadLoggingContext lhs, in ThreadLoggingContext rhs) => !(lhs > rhs);
        public override bool Equals(object? other) => other is ThreadLoggingContext tlc && tlc == this;
        public bool Equals(ThreadLoggingContext other) => other == this;
        public int CompareTo(ThreadLoggingContext other) => Compare(in this, in other);
        /// <inheritdoc />
        public override string ToString() => $"Thread Id: [{ManagedThreadId}]; Name: [{ThreadName}]";

        private static string GetNoNameThreadId()
        {
            long value = Interlocked.Increment(ref s_NoNameThreadCount);
            return string.Format(NoNameThreadFormatString, value);
        }

        #region Private Data
        [DataMember] private readonly string? _threadName;
        private static readonly TrimmedStringComparer TheComparer = TrimmedStringComparer.TrimmedOrdinal;
        private static long s_NoNameThreadCount = 0;
        private const string NoNameThreadFormatString = "Unnamed_Thread_{0}";

        #endregion
    }

    [DataContract]
    public sealed class LoggingContext
    {
        public static LoggingContext Current => LoggingContextSource.Value;
        public ref readonly ProcessLoggingContext ProcessContext => ref _plc;
        public ref readonly ThreadLoggingContext ThreadContext => ref _tlc;
        public string ProcessName => ProcessContext.ProcessName;
        public string ThreadName => ThreadContext.ThreadName;
        public int ThreadId => ThreadContext.ManagedThreadId;
        public int ProcessId => ProcessContext.ProcessId;

        private LoggingContext(in ThreadLoggingContext tlc, in ProcessLoggingContext plc)
        {
            _tlc = tlc;
            _plc = plc;
        }

        [DataMember] private readonly ThreadLoggingContext _tlc;
        [DataMember] private readonly ProcessLoggingContext _plc;

        private static class LoggingContextSource
        {
            internal static LoggingContext Value => SLoggingContext.Value;

            private static LoggingContext CreateLoggingContext()
            {
                ThreadLoggingContext tcl = ThreadLoggingContext.Create();
                return new LoggingContext(in tcl, in SPlc);
            }
            private static readonly ThreadLocal<LoggingContext> SLoggingContext = new ThreadLocal<LoggingContext>(CreateLoggingContext);
            private static readonly ProcessLoggingContext SPlc = ProcessLoggingContext.CreateContext();
        }
    }

    
}
