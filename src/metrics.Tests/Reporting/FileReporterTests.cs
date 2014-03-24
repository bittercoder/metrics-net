using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using metrics.Core;
using metrics.Reporting;
using metrics.Tests.Core;
using NUnit.Framework;

namespace metrics.Tests.Reporting
{
    [TestFixture]
    public class FileReporterTests
    {
        [SetUp]
        public void Setup()
        {
            _filename = Path.GetTempFileName();
        }

        string _filename;
        static Metrics _metrics;

        [TestFixtureTearDown]
        public void Cleanup()
        {
            if (File.Exists(_filename))
            {
                File.Delete(_filename);
            }
        }

        static void RegisterMetrics()
        {
            _metrics = new Metrics();

            CounterMetric counter = _metrics.Counter(typeof (CounterTests), "Can_run_with_known_counters_counter");
            counter.Increment(100);

            var queue = new Queue<int>();
            _metrics.Gauge(typeof (GaugeTests), "Can_run_with_known_counters_gauge", () => queue.Count);
            queue.Enqueue(1);
            queue.Enqueue(2);
        }

        [Test]
        public void Can_run_in_background()
        {
            const int ticks = 3;
            var block = new ManualResetEvent(false);

            RegisterMetrics();

            ThreadPool.QueueUserWorkItem(
                s =>
                {
                    using (var reporter = new FileReporter(_filename, _metrics))
                    {
                        reporter.Start(3, TimeUnit.Seconds);
                        while (true)
                        {
                            Thread.Sleep(1000);
                            int runs = reporter.Runs;
                            if (runs == ticks)
                            {
                                block.Set();
                            }
                        }
                    }
                });

            block.WaitOne(TimeSpan.FromSeconds(5));
        }

        [Test]
        public void Can_run_with_known_counters()
        {
            RegisterMetrics();

            using (var reporter = new FileReporter(_filename, _metrics))
            {
                reporter.Run();
            }
        }

        [Test]
        public void Can_stop()
        {
            var block = new ManualResetEvent(false);

            RegisterMetrics();

            ThreadPool.QueueUserWorkItem(
                s =>
                {
                    var reporter = new FileReporter(Path.GetTempFileName(), _metrics);
                    reporter.Start(1, TimeUnit.Seconds);
                    reporter.Stopped += delegate { block.Set(); };
                    Thread.Sleep(2000);
                    reporter.Stop();
                });

            block.WaitOne();
        }

        [Test]
        public void File_is_created_with_human_readable_content()
        {
            RegisterMetrics();

            using (var reporter = new FileReporter(_filename, _metrics))
            {
                reporter.Run();
                Assert.IsTrue(File.Exists(_filename));
            }

            string contents = File.ReadAllText(_filename);
            Console.WriteLine(contents);
        }

        [Test]
        public void File_is_created_with_json_content()
        {
            RegisterMetrics();

            using (var reporter = new FileReporter(_filename, new JsonReportFormatter(_metrics)))
            {
                reporter.Run();
                Assert.IsTrue(File.Exists(_filename));
            }

            string contents = File.ReadAllText(_filename);
            Console.WriteLine(contents);
        }
    }
}