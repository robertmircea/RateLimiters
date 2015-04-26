using System;

namespace Bert.RateLimiters
{
    public class FixedTokenBucket : TokenBucket
    {
        public FixedTokenBucket(long maxTokens, long refillInterval, long refillIntervalInMilliSeconds) : base(maxTokens, refillInterval, refillIntervalInMilliSeconds)
        {
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