using metrics.Core;
using NUnit.Framework;

namespace metrics.Tests.Core
{
    [TestFixture]
    public class CounterTests : MetricTestBase
    {
        [TearDown]
        public void TearDown()
        {
            _metrics.Clear();
        }

        readonly Metrics _metrics = new Metrics();

        [Test]
        public void Can_count()
        {
            CounterMetric counter = _metrics.Counter(typeof (CounterTests), "Can_count");
            Assert.IsNotNull(counter);

            counter.Increment(100);
            Assert.AreEqual(100, counter.Count);
        }
    }
}