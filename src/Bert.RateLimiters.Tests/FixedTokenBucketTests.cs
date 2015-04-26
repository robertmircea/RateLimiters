using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Bert.RateLimiters.Tests
{
    public class FixedTokenBucketTests
    {
        private FixedTokenBucket bucket;
        public const long MAX_TOKENS = 10;
        public const long REFILL_INTERVAL = 10;
        public const long N_LESS_THAN_MAX = 2;
        public const long N_GREATER_THAN_MAX = 12;
        private const int CUMULATIVE = 2;

        [SetUp]
        public void SetUp()
        {
            bucket = new FixedTokenBucket(MAX_TOKENS, REFILL_INTERVAL, 1000);
        }

        [Test]
        public void ShouldThrottle_WhenCalledWithNTokensLessThanMax_ReturnsFalse()
        {
            TimeSpan waitTime;
            var shouldThrottle = bucket.ShouldThrottle(N_LESS_THAN_MAX, out waitTime);

            Assert.That(shouldThrottle, Is.False);
            Assert.That(bucket.CurrentTokenCount, Is.EqualTo(MAX_TOKENS - N_LESS_THAN_MAX));
        }

        [Test]
        public void ShouldThrottle_WhenCalledWithNTokensGreaterThanMax_ReturnsTrue()
        {
            TimeSpan waitTime;
            var shouldThrottle = bucket.ShouldThrottle(N_GREATER_THAN_MAX, out waitTime);

            Assert.That(shouldThrottle, Is.True);
            Assert.That(waitTime, Is.EqualTo(TimeSpan.FromMilliseconds(REFILL_INTERVAL*1000)));
            Assert.That(bucket.CurrentTokenCount, Is.EqualTo(MAX_TOKENS));
        }


        [Test]
        public void ShouldThrottle_WhenCalledCumulativeNTimesIsLessThanMaxTokens_ReturnsFalse()
        {
            for (int i = 0; i < CUMULATIVE; i++)
            {
                TimeSpan waitTime;
                Assert.That(bucket.ShouldThrottle(N_LESS_THAN_MAX, out waitTime), Is.False);
                Assert.That(waitTime, Is.EqualTo(TimeSpan.Zero));
            }

            var tokens = bucket.CurrentTokenCount;

            Assert.That(tokens, Is.EqualTo(MAX_TOKENS - (CUMULATIVE * N_LESS_THAN_MAX)));
        }


        [Test]
        public void ShouldThrottle_WhenCalledCumulativeNTimesIsGreaterThanMaxTokens_ReturnsTrue()
        {
            
            for (int i = 0; i < CUMULATIVE; i++)
            {
                Assert.That(bucket.ShouldThrottle(N_GREATER_THAN_MAX), Is.True);
            }

            var tokens = bucket.CurrentTokenCount;

            Assert.That(tokens, Is.EqualTo(MAX_TOKENS));
        }


        [Test]
        public void ShouldThrottle_WhenCalledWithNLessThanMaxSleepNLessThanMax_ReturnsFalse()
        {
            SystemTime.SetCurrentTimeUtc = () => new DateTime(2014, 2, 27, 0, 0, 0, DateTimeKind.Utc);
            var virtualNow = SystemTime.UtcNow;

            var before = bucket.ShouldThrottle(N_LESS_THAN_MAX);
            var tokensBefore = bucket.CurrentTokenCount;
            Assert.That(before, Is.False);
            Assert.That(tokensBefore, Is.EqualTo(MAX_TOKENS - N_LESS_THAN_MAX));

            SystemTime.SetCurrentTimeUtc = () => virtualNow.AddSeconds(REFILL_INTERVAL);

            var after = bucket.ShouldThrottle(N_LESS_THAN_MAX);
            var tokensAfter = bucket.CurrentTokenCount;
            Assert.That(after, Is.False);
            Assert.That(tokensAfter, Is.EqualTo(MAX_TOKENS - N_LESS_THAN_MAX));

        }

        [Test]
        public void ShouldThrottle_WhenCalledWithNGreaterThanMaxSleepNGreaterThanMax_ReturnsTrue()
        {
            SystemTime.SetCurrentTimeUtc = () => new DateTime(2014, 2, 27, 0, 0, 0, DateTimeKind.Utc);
            var virtualNow = SystemTime.UtcNow;

            TimeSpan waitTime;
            var before = bucket.ShouldThrottle(N_GREATER_THAN_MAX, out waitTime);
            var tokensBefore = bucket.CurrentTokenCount;
            Assert.That(waitTime, Is.EqualTo(TimeSpan.FromSeconds(REFILL_INTERVAL)));
            Assert.That(before, Is.True);
            Assert.That(tokensBefore, Is.EqualTo(MAX_TOKENS));

            SystemTime.SetCurrentTimeUtc = () => virtualNow.AddSeconds(REFILL_INTERVAL+1);

            var after = bucket.ShouldThrottle(N_GREATER_THAN_MAX, out waitTime);
            var tokensAfter = bucket.CurrentTokenCount;
            Assert.That(after, Is.True);
            Assert.That(waitTime, Is.EqualTo(TimeSpan.FromSeconds(REFILL_INTERVAL)));
            Assert.That(tokensAfter, Is.EqualTo(MAX_TOKENS));
        }

        [Test]
        public void ShouldThrottle_WhenThrottle_WaitTimeIsDynamicallyCalculated()
        {
            var virtualTime = new DateTime(2014, 2, 27, 0, 0, 0, DateTimeKind.Utc);

            for (int i = 0; i < 3; i++)
            {
                int closureI = i;
                SystemTime.SetCurrentTimeUtc = () => virtualTime.AddSeconds(closureI*3);
                TimeSpan waitTime;
                bucket.ShouldThrottle(N_GREATER_THAN_MAX, out waitTime);
                Assert.That(waitTime, Is.EqualTo(TimeSpan.FromSeconds(10-i*3)));
            }

        }


        [Test]
        public void ShouldThrottle_WhenCalledWithNLessThanMaxSleepCumulativeNLessThanMax()
        {
            SystemTime.SetCurrentTimeUtc = () => new DateTime(2014, 2, 27, 0, 0, 0, DateTimeKind.Utc);
            var virtualNow = SystemTime.UtcNow;

            long sum = 0;
            for (int i = 0; i < CUMULATIVE; i++)
            {
                Assert.That(bucket.ShouldThrottle(N_LESS_THAN_MAX), Is.False);
                sum += N_LESS_THAN_MAX;
            }
            var tokensBefore = bucket.CurrentTokenCount;
            Assert.That(tokensBefore, Is.EqualTo(MAX_TOKENS - sum));

            SystemTime.SetCurrentTimeUtc = () => virtualNow.AddSeconds(REFILL_INTERVAL);

            for (int i = 0; i < CUMULATIVE; i++)
            {
                Assert.That(bucket.ShouldThrottle(N_LESS_THAN_MAX), Is.False);
            }
            var tokensAfter = bucket.CurrentTokenCount;
            Assert.That(tokensAfter, Is.EqualTo(MAX_TOKENS - sum));
        }

        [Test]
        public void ShouldThrottle_WhenCalledWithCumulativeNLessThanMaxSleepCumulativeNGreaterThanMax()
        {
            SystemTime.SetCurrentTimeUtc = () => new DateTime(2014, 2, 27, 0, 0, 0, DateTimeKind.Utc);
            var virtualNow = SystemTime.UtcNow;

            long sum = 0;
            for (int i = 0; i < CUMULATIVE; i++)
            {
                Assert.That(bucket.ShouldThrottle(N_LESS_THAN_MAX), Is.False);
                sum += N_LESS_THAN_MAX;
            }
            var tokensBefore = bucket.CurrentTokenCount;
            Assert.That(tokensBefore, Is.EqualTo(MAX_TOKENS - sum));

            SystemTime.SetCurrentTimeUtc = () => virtualNow.AddSeconds(REFILL_INTERVAL);

            for (int i = 0; i < 3*CUMULATIVE; i++)
            {
                bucket.ShouldThrottle(N_LESS_THAN_MAX);
            }

            var after = bucket.ShouldThrottle(N_LESS_THAN_MAX);
            var tokensAfter = bucket.CurrentTokenCount;

            Assert.That(after, Is.True);
            Assert.That(tokensAfter, Is.LessThan(N_LESS_THAN_MAX));
        }

        [Test]
        public void ShouldThrottle_WhenCalledWithCumulativeNGreaterThanMaxSleepCumulativeNLessThanMax()
        {
            SystemTime.SetCurrentTimeUtc = () => new DateTime(2014, 2, 27, 0, 0, 0, DateTimeKind.Utc);
            var virtualNow = SystemTime.UtcNow;

            for (int i = 0; i < 3*CUMULATIVE; i++)
                bucket.ShouldThrottle(N_LESS_THAN_MAX);

            var before = bucket.ShouldThrottle(N_LESS_THAN_MAX);
            var tokensBefore = bucket.CurrentTokenCount;

            Assert.That(before, Is.True);
            Assert.That(tokensBefore, Is.LessThan(N_LESS_THAN_MAX));

            SystemTime.SetCurrentTimeUtc = () => virtualNow.AddSeconds(REFILL_INTERVAL);

            long sum = 0;
            for (int i = 0; i < CUMULATIVE; i++)
            {
                Assert.That(bucket.ShouldThrottle(N_LESS_THAN_MAX), Is.False);
                sum += N_LESS_THAN_MAX;
            }

            var tokensAfter = bucket.CurrentTokenCount;
            Assert.That(tokensAfter, Is.EqualTo(MAX_TOKENS - sum));
        }


        [Test]
        public void ShouldThrottle_WhenCalledWithCumulativeNGreaterThanMaxSleepCumulativeNGreaterThanMax()
        {
            SystemTime.SetCurrentTimeUtc = () => new DateTime(2014, 2, 27, 0, 0, 0, DateTimeKind.Utc);
            var virtualNow = SystemTime.UtcNow;

            for (int i = 0; i < 3*CUMULATIVE; i++)
                bucket.ShouldThrottle(N_LESS_THAN_MAX);

            var before = bucket.ShouldThrottle(N_LESS_THAN_MAX);
            var tokensBefore = bucket.CurrentTokenCount;

            Assert.That(before, Is.True);
            Assert.That(tokensBefore, Is.LessThan(N_LESS_THAN_MAX));

            SystemTime.SetCurrentTimeUtc = () => virtualNow.AddSeconds(REFILL_INTERVAL);

            for (int i = 0; i < 3*CUMULATIVE; i++)
            {
                bucket.ShouldThrottle(N_LESS_THAN_MAX);
            }
            var after = bucket.ShouldThrottle(N_LESS_THAN_MAX);
            var tokensAfter = bucket.CurrentTokenCount;

            Assert.That(after, Is.True);
            Assert.That(tokensAfter, Is.LessThan(N_LESS_THAN_MAX));
        }

        [Test]
        public void ShouldThrottle_WhenThread1NLessThanMaxAndThread2NLessThanMax()
        {
            var t1 = new Thread(p =>
            {
                var throttle = bucket.ShouldThrottle(N_LESS_THAN_MAX);
                Assert.That(throttle, Is.False);
            });            
            
            var t2 = new Thread(p =>
            {
                var throttle = bucket.ShouldThrottle(N_LESS_THAN_MAX);
                Assert.That(throttle, Is.False);
            });

            t1.Start();
            t2.Start();

            t1.Join();
            t2.Join();

            Assert.That(bucket.CurrentTokenCount, Is.EqualTo(MAX_TOKENS - 2 * N_LESS_THAN_MAX));

        }
        
        [Test]
        public void ShouldThrottle_Thread1NGreaterThanMaxAndThread2NGreaterThanMax()
        {
            var shouldThrottle = bucket.ShouldThrottle(N_GREATER_THAN_MAX);
            Assert.That(shouldThrottle, Is.True);

            var t1 = new Thread(p =>
            {
                var throttle = bucket.ShouldThrottle(N_GREATER_THAN_MAX);
                Assert.That(throttle, Is.True);
            });            
            
            var t2 = new Thread(p =>
            {
                var throttle = bucket.ShouldThrottle(N_GREATER_THAN_MAX);
                Assert.That(throttle, Is.True);
            });

            t1.Start();
            t2.Start();

            t1.Join();
            t2.Join();

            Assert.That(bucket.CurrentTokenCount, Is.EqualTo(MAX_TOKENS));

        }
    }
}