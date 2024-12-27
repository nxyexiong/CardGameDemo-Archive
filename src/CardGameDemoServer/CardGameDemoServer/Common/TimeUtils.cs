using System;

namespace CardGameDemoServer.Common
{
    public class TimeUtils
    {
        public static long GetTimestampMs(DateTime dateTime)
        {
            return dateTime.Ticks / TimeSpan.TicksPerMillisecond;
        }
    }
}
