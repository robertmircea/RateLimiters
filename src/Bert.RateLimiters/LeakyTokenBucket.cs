using System;

namespace Bert.RateLimiters
{
    public abstract class LeakyTokenBucket : TokenBucket
    {
        protected readonly long stepTokens;
        protected long ticksStepInterval;

        protected LeakyTokenBucket(long maxTokens, long refillInterval, int refillIntervalInMilliSeconds, long stepTokens, long stepInterval, int stepIntervalInMilliseconds)
            : base(maxTokens, refillInterval, refillIntervalInMilliSeconds)
        {
            this.stepTokens = stepTokens;
            if (stepInterval < 0) throw new ArgumentOutOfRangeException("stepInterval", "Step interval cannot be negative");
            if (stepTokens < 0) throw new ArgumentOutOfRangeException("stepTokens", "Step tokens cannot be negative");
            if (stepIntervalInMilliseconds <= 0) throw new ArgumentOutOfRangeException("stepIntervalInMilliseconds", "Step interval in milliseconds cannot be negative");

            ticksStepInterval = TimeSpan.FromMilliseconds(stepInterval * stepIntervalInMilliseconds).Ticks;
        }

    }
}