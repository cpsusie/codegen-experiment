using System;
using MonotonicContext = HpTimeStamps.MonotonicStampContext;
using WallClockStamp = System.DateTime;
using HpStamp = System.DateTime;
namespace LoggerLibrary
{
    using MonotonicStamp = HpTimeStamps.MonotonicTimeStamp<MonotonicContext>;
    using MonoStampSource = HpTimeStamps.MonotonicTimeStampUtil<MonotonicContext>;
    public static class TimeStampProvider
    {
        public static ref readonly MonotonicContext Context => ref MonoStampSource.StampContext;
        public static MonotonicStamp MonoNow => MonoStampSource.StampNow;
        public static WallClockStamp WallStamp => WallClockStamp.UtcNow;
        public static bool IsHpCalibrationRequired => HpTimeStamps.TimeStampSource.NeedsCalibration;
        public static TimeSpan TimeSinceLastCalibration => HpTimeStamps.TimeStampSource.TimeSinceCalibration;
        public static void CalibrateNow() => HpTimeStamps.TimeStampSource.Calibrate();
        public static HpStamp HpNow => HpTimeStamps.TimeStampSource.UtcNow;

    }
}
