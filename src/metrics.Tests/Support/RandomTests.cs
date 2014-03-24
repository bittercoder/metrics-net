using System.Diagnostics;
using metrics.Support;
using NUnit.Framework;

namespace metrics.Tests.Support
{
    [TestFixture]
    public class RandomTests
    {
        [Test]
        public void Can_generate_random_longs()
        {
            for (int i = 0; i < 1000; i++)
            {
                long random = Random.NextLong();
                Trace.WriteLine(random);
            }
        }
    }
}