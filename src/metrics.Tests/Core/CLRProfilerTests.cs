using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using metrics.Core;
using metrics.Util;
using NUnit.Framework;

namespace metrics.Tests.Core
{
    [TestFixture]
    public class CLRProfilerTests
    {
        static void EnumerateCountersFor(string category, string instance)
        {
            var sb = new StringBuilder();
            var counterCategory = new PerformanceCounterCategory(category);
            foreach (PerformanceCounter counter in counterCategory.GetCounters(instance))
            {
                sb.AppendLine(string.Format("{0}:{1}:{2}", instance, category, counter.CounterName));
            }

            Console.WriteLine(sb.ToString());
        }

        static void EnumerateCountersFor(string category)
        {
            var sb = new StringBuilder();
            var counterCategory = new PerformanceCounterCategory(category);

            if (counterCategory.CategoryType == PerformanceCounterCategoryType.SingleInstance)
            {
                foreach (PerformanceCounter counter in counterCategory.GetCounters())
                {
                    sb.AppendLine(string.Format("{0}:{1}", category, counter.CounterName));
                }
            }
            else
            {
                foreach (string counterInstance in counterCategory.GetInstanceNames())
                {
                    foreach (PerformanceCounter counter in counterCategory.GetCounters(counterInstance))
                    {
                        sb.AppendLine(string.Format("{0}:{1}:{2}", counterInstance, category, counter.CounterName));
                    }
                }
            }

            Console.WriteLine(sb.ToString());
        }

        static void EnumerateCounters()
        {
            string[] categories = PerformanceCounterCategory.GetCategories().Select(c => c.CategoryName).OrderBy(s => s).ToArray();

            var sb = new StringBuilder();

            foreach (string category in categories)
            {
                var counterCategory = new PerformanceCounterCategory(category);

                foreach (string counterInstance in counterCategory.GetInstanceNames())
                {
                    try
                    {
                        foreach (PerformanceCounter counter in counterCategory.GetCounters(counterInstance))
                        {
                            sb.AppendLine(string.Format("{0}:{1}:{2}", counterInstance, category, counter.CounterName));
                        }
                    }
                    catch
                    {
                        // Drop it on the floor
                    }
                }
            }

            Console.WriteLine(sb.ToString());
        }

        static void AssertProfilerHasValue(double heap)
        {
            Assert.IsNotNull(heap);
            Trace.WriteLine(heap);
        }

        [Test]
        public void Can_dump_tracked_threads()
        {
            var factory = new NamedThreadFactory("Can_dump_managed_threads");

            Thread thread = factory.New(() =>
            {
                while (true)
                {
                    Debug.Assert("This".Equals("This"));
                }
            });

            thread.Start();

            Console.WriteLine(CLRProfiler.DumpTrackedThreads());
        }

        [Test]
        public void Can_enumerate_all_counters()
        {
            EnumerateCounters();
        }

        [Test]
        public void Can_enumerate_machine_categories()
        {
            // http://technet.microsoft.com/en-us/library/cc768048.aspx
            EnumerateCountersFor("System");
            EnumerateCountersFor("Processor");
            EnumerateCountersFor("Memory");
            EnumerateCountersFor("Network Interface");
            EnumerateCountersFor("PhysicalDisk", "_Total");
            EnumerateCountersFor("LogicalDisk", "_Total");
        }

        [Test]
        public void Can_get_machine_metrics()
        {
            double value = CLRProfiler.GlobalTotalNumberOfContentions;
            AssertProfilerHasValue(value);

            value = CLRProfiler.GlobalContentionRatePerSecond;
            AssertProfilerHasValue(value);

            value = CLRProfiler.GlobalCurrentQueueLength;
            AssertProfilerHasValue(value);

            value = CLRProfiler.GlobalQueueLengthPeak;
            AssertProfilerHasValue(value);
        }
    }
}