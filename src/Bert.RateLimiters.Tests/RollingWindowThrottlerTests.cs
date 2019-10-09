using System;
using NUnit.Framework;
using Shouldly;

namespace Bert.RateLimiters.Tests
{
    public class RollingWindowThrottlerTests
    {
        private readonly DateTime referenceTime = new DateTime(2014, 9, 20, 0, 0, 0, DateTimeKind.Utc);

        [Test]
        public void Throws_WhenNumberOfOccurencesIsLesserThanOne()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>new RollingWindowThrottler(-1, TimeSpan.FromSeconds(5)));
        }

        [Test]
        public void ShouldThrottle_WhenCalledWithTokensLessThanOne_WillThrow()
        {
            var throttler = new RollingWindowThrottler(1, TimeSpan.FromSeconds(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => throttler.ShouldThrottle(0, out _));
        }

        [Test]
        public void ShouldThrottle_WhenCalled_WillReturnFalse()
        {
            var throttler = new RollingWindowThrottler(1, TimeSpan.FromSeconds(1));
            var shouldThrottle = throttler.ShouldThrottle(1, out _);

            shouldThrottle.ShouldBeFalse();
        }

        [Test]
        public void ShouldThrottle_WhenCalledTwiceinSameSecondAndAllows1PerSecond_WillReturnTrue()
        {

            SystemTime.SetCurrentTimeUtc = () => referenceTime;
            var virtualNow = SystemTime.UtcNow;

            var throttler = new RollingWindowThrottler(1, TimeSpan.FromSeconds(1));
            long waitTimeMillis;
            var shouldThrottle = throttler.ShouldThrottle(1, out waitTimeMillis);
            shouldThrottle.ShouldBeFalse();

            SystemTime.SetCurrentTimeUtc = () => virtualNow.AddSeconds(0.5);
            shouldThrottle = throttler.ShouldThrottle(1, out waitTimeMillis);
            shouldThrottle.ShouldBeTrue();
            waitTimeMillis.ShouldBe(500);

            SystemTime.SetCurrentTimeUtc = () => virtualNow.AddSeconds(0.8);
            shouldThrottle = throttler.ShouldThrottle(1, out waitTimeMillis);
            shouldThrottle.ShouldBeTrue();
            waitTimeMillis.ShouldBe(200);
        }


        [Test]
        public void ShouldThrottle_WhenCalledAfterSecondPassAndAllows1PerSecond_WillReturnFalse()
        {

            SystemTime.SetCurrentTimeUtc = () => referenceTime;
            var virtualNow = SystemTime.UtcNow;

            var throttler = new RollingWindowThrottler(1, TimeSpan.FromSeconds(1));
            var shouldThrottle = throttler.ShouldThrottle(1, out _);
            shouldThrottle.ShouldBeFalse();

            SystemTime.SetCurrentTimeUtc = () => virtualNow.AddSeconds(1);
            shouldThrottle = throttler.ShouldThrottle(1, out _);
            shouldThrottle.ShouldBeFalse();
        }        
        
        [Test]
        public void ShouldThrottle_WhenCalledTwiceinSameSecondAndAllows2PerSecond_WillReturnFalse()
        {

            SystemTime.SetCurrentTimeUtc = () => referenceTime;
            var virtualNow = SystemTime.UtcNow;

            var throttler = new RollingWindowThrottler(2, TimeSpan.FromSeconds(1));
            var shouldThrottle = throttler.ShouldThrottle(1, out _);
            shouldThrottle.ShouldBeFalse();

            SystemTime.SetCurrentTimeUtc = () => virtualNow.AddSeconds(0.5);
            shouldThrottle = throttler.ShouldThrottle(1, out _);
            shouldThrottle.ShouldBeFalse();
        }

        [Test]
        public void ShouldThrottle_WhenCalledAfterSecondPassesAndAllows2PerSecond_WillReturnFalse()
        {

            SystemTime.SetCurrentTimeUtc = () => referenceTime;
            var virtualNow = SystemTime.UtcNow;

            var throttler = new RollingWindowThrottler(2, TimeSpan.FromSeconds(1));
            long waitTimeMillis;
            var shouldThrottle = throttler.ShouldThrottle(1, out waitTimeMillis);
            shouldThrottle.ShouldBeFalse();

            SystemTime.SetCurrentTimeUtc = () => virtualNow.AddSeconds(1);
            shouldThrottle = throttler.ShouldThrottle(1, out waitTimeMillis);
            shouldThrottle.ShouldBeFalse();
            waitTimeMillis.ShouldBe(0);
        }

        [Test]
        public void ShouldThrottle_WhenCalledThreeTimesinSameSecondAndAllows2PerSecond_WillReturnTrue()
        {

            SystemTime.SetCurrentTimeUtc = () => referenceTime;
            var virtualNow = SystemTime.UtcNow;

            var throttler = new RollingWindowThrottler(2, TimeSpan.FromSeconds(1));
            long waitTimeMillis;
            var shouldThrottle = throttler.ShouldThrottle(1, out waitTimeMillis);
            shouldThrottle.ShouldBeFalse();

            SystemTime.SetCurrentTimeUtc = () => virtualNow.AddSeconds(0.5);
            shouldThrottle = throttler.ShouldThrottle(1, out waitTimeMillis);
            shouldThrottle.ShouldBeFalse();

            SystemTime.SetCurrentTimeUtc = () => virtualNow.AddSeconds(0.7);
            shouldThrottle = throttler.ShouldThrottle(1, out waitTimeMillis);
            shouldThrottle.ShouldBeTrue();
            waitTimeMillis.ShouldBe(300);

        }


        [Test]
        public void ShouldThrottle_WhenCalledAtEndOfRollingWindowAndAllows2PerSecond_WillReturnFalse()
        {

            SystemTime.SetCurrentTimeUtc = () => referenceTime;
            var virtualNow = SystemTime.UtcNow;

            var throttler = new RollingWindowThrottler(2, TimeSpan.FromSeconds(1));
            var shouldThrottle = throttler.ShouldThrottle(1, out _);
            shouldThrottle.ShouldBeFalse();

            //first rolling window expired, init a new one
            SystemTime.SetCurrentTimeUtc = () => virtualNow.AddSeconds(1.2);
            shouldThrottle = throttler.ShouldThrottle(1, out _);
            shouldThrottle.ShouldBeFalse();

            //inside second rolling window, under threshold
            SystemTime.SetCurrentTimeUtc = () => virtualNow.AddSeconds(1.3);
            shouldThrottle = throttler.ShouldThrottle(1, out _);
            shouldThrottle.ShouldBeFalse();

            //second rolling window expired, beginning third window
            SystemTime.SetCurrentTimeUtc = () => virtualNow.AddSeconds(2.2);
            shouldThrottle = throttler.ShouldThrottle(1, out _);
            shouldThrottle.ShouldBeFalse();

            //third window, under threshold
            SystemTime.SetCurrentTimeUtc = () => virtualNow.AddSeconds(2.3);
            shouldThrottle = throttler.ShouldThrottle(1, out _);
            shouldThrottle.ShouldBeFalse();

        }

        [Test]
        public void ShouldThrottle_WhenCalledWithMoreTokensThanOccurrences_WillReturnTrue()
        {
            var throttler = new RollingWindowThrottler(2, TimeSpan.FromSeconds(1));
            var shouldThrottle = throttler.ShouldThrottle(3, out _);
            shouldThrottle.ShouldBeTrue();            
        }

        [Test]
        public void ShouldThrottle_WhenCalledAndConsumingAllTokensAtOnce_WillReturnFalse()
        {
            var throttler = new RollingWindowThrottler(3, TimeSpan.FromSeconds(1));
            var shouldThrottle = throttler.ShouldThrottle(3, out _);
            shouldThrottle.ShouldBeFalse();
        }

        [Test]
        public void ShouldThrottle_WhenCalledAndConsumingAllTokensAtOnceAndThenCalledOnceMore_WillReturnTrue()
        {
            SystemTime.SetCurrentTimeUtc = () => referenceTime;
            var virtualNow = SystemTime.UtcNow;

            var throttler = new RollingWindowThrottler(3, TimeSpan.FromSeconds(1));
            long waitTimeMillis;
            var shouldThrottle = throttler.ShouldThrottle(3, out waitTimeMillis);
            shouldThrottle.ShouldBeFalse();

            SystemTime.SetCurrentTimeUtc = () => virtualNow.AddSeconds(0.2);
            shouldThrottle = throttler.ShouldThrottle(3, out waitTimeMillis);
            shouldThrottle.ShouldBeTrue();
            waitTimeMillis.ShouldBe(800);
        }


        [Test]
        public void ShouldThrottle_WhenCalledAndConsumingAllTokensAtOnceAndThenCalledOnceMoreAfterRollingWindowEnd_WillReturnFalse()
        {
            SystemTime.SetCurrentTimeUtc = () => referenceTime;
            var virtualNow = SystemTime.UtcNow;

            var throttler = new RollingWindowThrottler(3, TimeSpan.FromSeconds(1));
            long waitTimeMillis;
            var shouldThrottle = throttler.ShouldThrottle(3, out waitTimeMillis);
            shouldThrottle.ShouldBeFalse();

            SystemTime.SetCurrentTimeUtc = () => virtualNow.AddSeconds(1.1);
            shouldThrottle = throttler.ShouldThrottle(3, out waitTimeMillis);
            shouldThrottle.ShouldBeFalse();
            waitTimeMillis.ShouldBe(0);
        }

    }
}