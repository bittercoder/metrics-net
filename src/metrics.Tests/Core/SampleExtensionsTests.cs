using System;
using metrics.Core;
using NUnit.Framework;

namespace metrics.Tests.Core
{
    [TestFixture]
    public class SampleExtensionsTests
    {
        [Test]
        public void NewSample_ForEachSampleType_DoesNotThrow()
        {
            foreach (HistogramMetric.SampleType sampleType in (HistogramMetric.SampleType[]) Enum.GetValues(typeof (HistogramMetric.SampleType)))
                sampleType.NewSample();
        }
    }
}