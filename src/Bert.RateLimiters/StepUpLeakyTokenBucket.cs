using System;

namespace Bert.RateLimiters
{
    public class StepUpLeakyTokenBucket : LeakyTokenBucket
    {
        private long lastActivityTime;

        public StepUpLeakyTokenBucket(long maxTokens, long refillInterval, int refillIntervalInMilliSeconds, long stepTokens, long stepInterval, int stepIntervalInMilliseconds) : base(maxTokens, refillInterval, refillIntervalInMilliSeconds, stepTokens, stepInterval, stepIntervalInMilliseconds)
        {
        }

        protected override void UpdateTokens()
        {
            var currentTime = SystemTime.UtcNow.Ticks;

            if (currentTime >= nextRefillTime)
            {
                tokens = stepTokens;

                lastActivityTime = currentTime;
                nextRefillTime = currentTime + ticksRefillInterval;

                return;
            }

            //calculate tokens at current step

            long elapsedTimeSinceLastActivity = currentTime - lastActivityTime;
            long elapsedStepsSinceLastActivity = elapsedTimeSinceLastActivity / ticksStepInterval;

            tokens += (elapsedStepsSinceLastActivity*stepTokens);

            if (tokens > bucketTokenCapacity) tokens = bucketTokenCapacity;
            lastActivityTime = currentTime;
        }
    }
}