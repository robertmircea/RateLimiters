using System;

namespace Bert.RateLimiters
{
    public interface IThrottleStrategy
    {
        bool ShouldThrottle(long n = 1);
        bool ShouldThrottle(long n, out TimeSpan waitTime);
        bool ShouldThrottle(out TimeSpan waitTime);
        long CurrentTokenCount { get; }
    }
}