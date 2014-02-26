using System;

namespace Bert.RateLimiters
{
    public class FixedTokenBucket : TokenBucket
    {
        private readonly long ticksRefillInterval;
        private long nextRefillTime;

        public FixedTokenBucket(long maxTokens, long refillInterval, long refillIntervalInMilliSeconds) : base(maxTokens)
        {
            if (refillInterval < 0) throw new ArgumentOutOfRangeException("refillInterval", "Refill interval cannot be negative");
            if (refillIntervalInMilliSeconds <= 0) throw new ArgumentOutOfRangeException("refillIntervalInMilliSeconds", "Refill interval in milliseconds cannot be negative");

            ticksRefillInterval = TimeSpan.FromMilliseconds(refillInterval * refillIntervalInMilliSeconds).Ticks;
        }

        protected override void UpdateTokens()
        {
            var currentTime = SystemTime.UtcNow.Ticks;

            if (currentTime < nextRefillTime) return;

            tokens = bucketTokenCapacity;
            nextRefillTime = currentTime + ticksRefillInterval;
        }
    }
}