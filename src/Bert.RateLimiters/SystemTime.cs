using System;

namespace Bert.RateLimiters
{
    public static class SystemTime
    {
        // Allow modification of "Today" for unit testing
        public static Func<DateTime> SetCurrentTimeUtc = () => DateTime.UtcNow;
        public static Func<DateTime> SetCurrentTime = () => DateTime.Now;
        public static Func<int> SetTickCount = () => Environment.TickCount;

        public static DateTime UtcNow
        {
            get
            {
                return SetCurrentTimeUtc();
            }
        }

        public static DateTime Now
        {
            get
            {
                return SetCurrentTime();
            }
        }

        public static int EnvironmentTickCount
        {
            get { return SetTickCount(); }
        }
    }
}