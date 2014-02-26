namespace Bert.RateLimiters
{
    public interface IThrottleStrategy
    {
        bool ShouldThrottle(long n);
        bool ShouldThrottle();
        bool ShouldThrottle(long n, out int waitTime);
        bool ShouldThrottle(out int waitTime);
        long CurrentTokenCount { get; }
    }
}