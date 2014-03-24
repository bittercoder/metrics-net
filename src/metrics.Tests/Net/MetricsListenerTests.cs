using System.Diagnostics;
using System.IO;
using System.Net;
using metrics.Core;
using metrics.Net;
using NUnit.Framework;

namespace metrics.Tests.Net
{
    [TestFixture]
    public class MetricsListenerTests
    {
        MetricsListener _listener;
        const int Port = 9898;
        readonly Metrics _metrics = new Metrics();


        [TestFixtureSetUp]
        public void SetUp()
        {
            _listener = new MetricsListener(_metrics);
            _listener.Start(Port);
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            _listener.Stop();
        }

        static string GetResponseForRequest(string url, string accept = "application/json")
        {
            try
            {
                var request = (HttpWebRequest) WebRequest.Create(url);
                request.Accept = accept;

                WebResponse response = request.GetResponse();
                using (var sr = new StreamReader(response.GetResponseStream()))
                {
                    string result = sr.ReadToEnd();
                    return result;
                }
            }
            catch (WebException ex)
            {
                if (ex.Response is HttpWebResponse)
                {
                    using (var esr = new StreamReader(ex.Response.GetResponseStream()))
                    {
                        return esr.ReadToEnd();
                    }
                }

                return null;
            }
        }

        [Test]
        public void Can_request_base_url_in_html()
        {
            string content = GetResponseForRequest("http://localhost:" + Port + "/", "text/html");

            Trace.WriteLine(content);
        }

        [Test]
        public void Can_respond_to_metrics_request_when_metrics_are_registered()
        {
            _metrics.Clear();

            CounterMetric counter = _metrics.Counter(typeof (MetricsListenerTests), "counter");

            counter.Increment();

            string content = GetResponseForRequest("http://localhost:" + Port + "/metrics");
            const string expected = @"[{""name"":""counter"",""metric"":{""count"":1}}]";
            Assert.AreEqual(expected, content);
        }

        [Test]
        public void Can_respond_to_metrics_request_when_no_metrics_are_registered()
        {
            _metrics.Clear();

            string content = GetResponseForRequest("http://localhost:" + Port + "/metrics");

            Assert.AreEqual("[]", content);
        }

        [Test]
        public void Can_respond_to_ping_request()
        {
            string content = GetResponseForRequest("http://localhost:" + Port + "/ping");

            Assert.AreEqual("pong", content);
        }

        [Test]
        public void Can_respond_with_not_found_with_body_when_path_is_not_found()
        {
            string content = GetResponseForRequest("http://localhost:" + Port + "/unknown");

            Assert.AreEqual("<!doctype html><html><body>Resource not found</body></html>", content);
        }

        [Test]
        public void Can_stop_gracefully()
        {
            Can_respond_to_ping_request();
            _listener.Stop();
            _listener.Stop();
        }
    }
}