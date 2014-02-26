using System;

namespace Bert.RateLimiters
{
    public abstract class LeakyTokenBucket : TokenBucket
    {
        protected readonly long stepTokens;
        protected long ticksRefillInterval;
        protected long ticksStepInterval;
        protected long nextRefillTime;

        protected LeakyTokenBucket(long maxTokens, long refillInterval, int refillIntervalInMilliSeconds, long stepTokens, long stepInterval, int stepIntervalInMilliseconds)
            : base(maxTokens)
        {
            this.stepTokens = stepTokens;
            if (refillInterval<0) throw new ArgumentOutOfRangeException("refillInterval", "Refill interval cannot be negative");
            if (stepInterval < 0) throw new ArgumentOutOfRangeException("stepInterval", "Step interval cannot be negative");
            if (stepTokens < 0) throw new ArgumentOutOfRangeException("stepTokens", "Step tokens cannot be negative");
            if (refillIntervalInMilliSeconds <= 0) throw new ArgumentOutOfRangeException("refillIntervalInMilliSeconds", "Refill interval in milliseconds cannot be negative");
            if (stepIntervalInMilliseconds <= 0) throw new ArgumentOutOfRangeException("stepIntervalInMilliseconds", "Step interval in milliseconds cannot be negative");

            ticksRefillInterval = TimeSpan.FromMilliseconds(refillInterval * refillIntervalInMilliSeconds).Ticks;
            ticksStepInterval = TimeSpan.FromMilliseconds(stepInterval * stepIntervalInMilliseconds).Ticks;
        }

    }
}