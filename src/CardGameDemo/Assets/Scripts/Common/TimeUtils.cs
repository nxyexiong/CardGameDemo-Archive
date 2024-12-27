using System;


namespace Common
{
    public class TimeUtils
    {
        public static long GetTimestampMs(DateTime dateTime)
        {
            return dateTime.Ticks / TimeSpan.TicksPerMillisecond;
        }
    }
}
