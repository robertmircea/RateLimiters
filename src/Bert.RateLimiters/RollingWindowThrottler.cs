using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Bert.RateLimiters
{
    /// <summary>
    /// Used to control the rate of reservations/occurrences per unit of time.
    /// </summary>
    /// <remarks>
    /// The algorithm uses a rolling time window so that precise control over the rate is achieved (without any bursts)
    /// </remarks>
    public class RollingWindowThrottler
    {
        private readonly int occurrences;
        private readonly long timeUnitTicks;
        private readonly object syncRoot = new object();
        private int remainingTokens;
        private readonly Queue<long> exitTimesInTicksQueue = new Queue<long>();
        private long nextCheckTime;

        /// <summary>
        /// Constructs an instance of the throttler.
        /// </summary>
        /// <param name="occurrences">Maximum number of reservation which can be made</param>
        /// <param name="timeUnit">The time unit in which the reservations can be made</param>
        public RollingWindowThrottler(int occurrences, TimeSpan timeUnit)
        {
            if(occurrences<=0)
                throw new ArgumentOutOfRangeException("occurrences", "Number of occurences must be a positive integer");


            this.occurrences = occurrences;
            timeUnitTicks = timeUnit.Ticks;
            remainingTokens = occurrences;
        }


        /// <summary>
        /// Total number of reservations which can be made for a time unit
        /// </summary>
        public int Occurrences
        {
            get { return occurrences; }
        }

        /// <summary>
        /// The time unit in which the maximal number of <see cref="Occurrences"/> can be reserved.
        /// </summary>
        public TimeSpan TimeUnit
        {
            get { return TimeSpan.FromTicks(timeUnitTicks); }
        }

        /// <summary>
        /// Returns the number of available tokens which can be reserved until the end of current time unit
        /// </summary>
        public int AvailableTokens
        {
            get
            {
                lock (syncRoot)
                {
                    return remainingTokens;
                }
            }
        }

        /// <summary>
        /// Tries to reserve one token in the configured time unit.
        /// </summary>
        /// <param name="waitTimeMillis">total suggested wait time till tokens will become available for reservation</param>
        /// <returns>true if the caller should throttle/wait, or false if reservation was made successfully.</returns>
        public bool ShouldThrottle(out long waitTimeMillis)
        {
            return ShouldThrottle(1, out waitTimeMillis);
        }


        /// <summary>
        /// Tries to reserve <see cref="tokens"/> in the configured time unit.
        /// </summary>
        /// <param name="tokens">total number of reservations</param>
        /// <param name="waitTimeMillis">total suggested wait time till tokens will become available for reservation</param>
        /// <returns>true if the caller should throttle/wait, or false if reservation was made successfully.</returns>
        public bool ShouldThrottle(int tokens, out long waitTimeMillis)
        {
            if (tokens <= 0) throw new ArgumentOutOfRangeException("tokens", "Should be positive integer greater than 0");
            var currentTime = SystemTime.UtcNow.Ticks;

            lock (syncRoot)
            {
                CheckExitTimeQueue();
                if (remainingTokens - tokens >= 0)
                {
                    remainingTokens -= tokens;
                    waitTimeMillis = 0;
                    long timeToExit = unchecked (currentTime + timeUnitTicks);
                    for (int i = 0; i < tokens; i++)
                        exitTimesInTicksQueue.Enqueue(timeToExit);
                    return false;
                }

                waitTimeMillis = (nextCheckTime - currentTime) / TimeSpan.TicksPerMillisecond;
                return true;
            }
        }

        public void CheckExitTimeQueue()
        {
            if(nextCheckTime > SystemTime.UtcNow.Ticks)
                return;

            while (exitTimesInTicksQueue.Count > 0 && exitTimesInTicksQueue.Peek() <= SystemTime.UtcNow.Ticks)
            {
                exitTimesInTicksQueue.Dequeue();
                remainingTokens++;
            }

            //try to determine next check time
            if (exitTimesInTicksQueue.Count > 0)
            {
                var item = exitTimesInTicksQueue.Peek();
                nextCheckTime = item;
            }
            else
                nextCheckTime = timeUnitTicks;
        }
        
    }
}