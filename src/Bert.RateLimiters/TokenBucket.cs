using System;

namespace Bert.RateLimiters
{
    public abstract class TokenBucket : IThrottleStrategy
    {
        protected long bucketTokenCapacity;
        private readonly object syncRoot = new object();


        //number of tokens in the bucket
        protected long tokens;

        protected TokenBucket(long bucketTokenCapacity)
        {
            if(bucketTokenCapacity <= 0) throw new ArgumentException("bucket token capacity can not be negative");
            this.bucketTokenCapacity = bucketTokenCapacity;
        }

        public bool ShouldThrottle(long n)
        {
            int waitTime;
            return ShouldThrottle(n, out waitTime);
        }

        public bool ShouldThrottle()
        {
            int waitTime;
            return ShouldThrottle(1, out waitTime);
        }

        public bool ShouldThrottle(long n, out int waitTime)
        {
            if(n<=0) throw new ArgumentException("Should be positive integer", "n");

            lock (syncRoot)
            {
                UpdateTokens();
                if (tokens < n)
                {
                    waitTime = 0;
                    return true;
                }
                tokens -= n;

                waitTime = 0;
                return false;
            }
        }

        protected abstract void UpdateTokens();

        public bool ShouldThrottle(out int waitTime)
        {
            return ShouldThrottle(1, out waitTime);
        }

        public long CurrentTokenCount
        {
            get
            {
                lock (syncRoot)
                {
                    UpdateTokens();
                    return tokens;
                }
            }
        }
    }
}