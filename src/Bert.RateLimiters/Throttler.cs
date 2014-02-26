using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bert.RateLimiters
{
    public class Throttler
    {
        private readonly IThrottleStrategy strategy;

        public Throttler(IThrottleStrategy strategy)
        {
            if (strategy == null) throw new ArgumentNullException("strategy");
            this.strategy = strategy;
        }

        public bool CanConsume()
        {
            return !strategy.ShouldThrottle();
        }
    }
}
