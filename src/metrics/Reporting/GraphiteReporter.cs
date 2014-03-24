using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using metrics.Core;
using metrics.Reporting;

namespace metrics.graphite
{
    public class GraphiteReporter : IReporter
    {
        readonly string host;
        readonly int port;
        readonly string prefix;
        readonly Metrics metrics;

        protected CancellationTokenSource Token;

        public GraphiteReporter(string host, int port, string prefix, Metrics metrics)
        {
            this.host = host;
            this.port = port;
            if (!prefix.EndsWith(".")) prefix = prefix + ".";
            this.prefix = prefix;
            this.metrics = metrics;
        }

        public int Runs { get; set; }

        public virtual void Start(long period, TimeUnit unit)
        {
            long seconds = unit.Convert(period, TimeUnit.Seconds);
            TimeSpan interval = TimeSpan.FromSeconds(seconds);

            Token = new CancellationTokenSource();
            Task.Factory.StartNew(async () =>
            {
                OnStarted();
                while (!Token.IsCancellationRequested)
                {
                    await Task.Delay(interval, Token.Token);
                    if (!Token.IsCancellationRequested)
                        Run();
                }
            }, Token.Token);
        }

        public void Stop()
        {
            Token.Cancel();
            OnStopped();
        }

        public event EventHandler<EventArgs> Started;

        public event EventHandler<EventArgs> Stopped;

        public void Dispose()
        {
            if (Token != null)
            {
                Token.Cancel();
            }
        }

        public void Run()
        {
            try
            {
                long epoch = DateTime.Now.ToUnixTime();
                using (var tcpclient = new TcpClient(host, port))
                using (NetworkStream stream = tcpclient.GetStream())
                using (var writer = new StreamWriter(stream))
                {
                    PrintRegularMetrics(writer, epoch);
                    writer.Flush();
                    Runs++;
                }
            }
            catch (Exception e)
            {
                Trace.TraceError("Error occured writing to graphite: " + e.Message);
                Trace.TraceError(e.ToString());
            }
        }

        public void OnStarted()
        {
            EventHandler<EventArgs> handler = Started;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public void OnStopped()
        {
            EventHandler<EventArgs> handler = Stopped;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        void PrintRegularMetrics(StreamWriter writer, long epoch)
        {
            foreach (var metric in metrics.All)
            {
                if (metric.Value is GaugeMetric)
                    ProcessGauge(writer, metric.Key, metric.Value as GaugeMetric, epoch);
                else if (metric.Value is CounterMetric)
                    ProcessCounter(writer, metric.Key, metric.Value as CounterMetric, epoch);
                else if (metric.Value is MeterMetric)
                    ProcessMeter(writer, metric.Key, metric.Value as MeterMetric, epoch);
                else if (metric.Value is HistogramMetric)
                    ProcessHistogram(writer, metric.Key, metric.Value as HistogramMetric, epoch);
                else if (metric.Value is TimerMetricBase)
                    ProcessTimer(writer, metric.Key, metric.Value as TimerMetric, epoch);
                else
                    throw new InvalidOperationException(string.Format("Invalid Metric type {0}", metric.Value));
            }
        }

        void ProcessGauge(StreamWriter writer, MetricName name, GaugeMetric gauge, long epoch)
        {
            SendObjToGraphite(writer, epoch, SanitizeName(name), "value", gauge.ValueAsString);
        }

        void ProcessCounter(StreamWriter writer, MetricName name, CounterMetric counter, long epoch)
        {
            SendObjToGraphite(writer, epoch, SanitizeName(name), "count", counter.Count);
        }

        void ProcessMeter(StreamWriter writer, MetricName name, IMetered meter, long epoch)
        {
            string sanitizedName = SanitizeName(name);
            SendObjToGraphite(writer, epoch, sanitizedName, "count", meter.Count);
            SendObjToGraphite(writer, epoch, sanitizedName, "meanRate", meter.MeanRate);
            SendObjToGraphite(writer, epoch, sanitizedName, "1MinuteRate", meter.OneMinuteRate);
            SendObjToGraphite(writer, epoch, sanitizedName, "5MinuteRate", meter.FiveMinuteRate);
            SendObjToGraphite(writer, epoch, sanitizedName, "15MinuteRate", meter.FifteenMinuteRate);
        }

        void ProcessHistogram(StreamWriter writer, MetricName name, HistogramMetric histogram, long epoch)
        {
            string sanitizedName = SanitizeName(name);
            sendFloat(writer, epoch, sanitizedName, "min", histogram.Min);
            sendFloat(writer, epoch, sanitizedName, "max", histogram.Max);
            sendFloat(writer, epoch, sanitizedName, "mean", histogram.Mean);
            sendFloat(writer, epoch, sanitizedName, "stddev", histogram.StdDev);

            double[] percentiles = histogram.Percentiles(0.5, 0.75, 0.95, 0.98, 0.99, 0.999);
            SendSampling(writer, epoch, sanitizedName, percentiles);
        }

        void ProcessTimer(StreamWriter writer, MetricName name, TimerMetric timer, long epoch)
        {
            ProcessMeter(writer, name, timer, epoch);
            string sanitizedName = SanitizeName(name);
            sendFloat(writer, epoch, sanitizedName, "min", timer.Min);
            sendFloat(writer, epoch, sanitizedName, "max", timer.Max);
            sendFloat(writer, epoch, sanitizedName, "mean", timer.Mean);
            sendFloat(writer, epoch, sanitizedName, "stddev", timer.StdDev);

            double[] percentiles = timer.Percentiles(0.5, 0.75, 0.95, 0.98, 0.99, 0.999);
            SendSampling(writer, epoch, sanitizedName, percentiles);
        }

        void SendSampling(StreamWriter writer, long epoch, string sanitizedName, double[] percentiles)
        {
            sendFloat(writer, epoch, sanitizedName, "median", percentiles[0]);
            sendFloat(writer, epoch, sanitizedName, "75percentile", percentiles[1]);
            sendFloat(writer, epoch, sanitizedName, "95percentile", percentiles[2]);
            sendFloat(writer, epoch, sanitizedName, "98percentile", percentiles[3]);
            sendFloat(writer, epoch, sanitizedName, "99percentile", percentiles[4]);
            sendFloat(writer, epoch, sanitizedName, "999percentile", percentiles[5]);
        }

        protected String SanitizeName(MetricName name)
        {
            return name.ToString();
        }

        protected String SanitizeString(String s)
        {
            return s.Replace(' ', '-');
        }

        protected void sendInt(StreamWriter writer, long timestamp, String name, String valueName, long value)
        {
            SendToGraphite(writer, timestamp, name, valueName + " " + value.ToString("{0}"));
        }

        protected void sendFloat(StreamWriter writer, long timestamp, String name, String valueName, double value)
        {
            SendToGraphite(writer, timestamp, name, valueName + " " + value.ToString("{0:2}"));
        }

        protected void SendObjToGraphite(StreamWriter writer, long timestamp, String name, String valueName, Object value)
        {
            SendToGraphite(writer, timestamp, name, valueName + " " + value);
        }

        protected void SendToGraphite(StreamWriter writer, long timestamp, String name, String value)
        {
            if (prefix != null) writer.Write(prefix);
            writer.Write(SanitizeString(name));
            writer.Write('.');
            writer.Write(value);
            writer.Write(' ');
            writer.Write(timestamp.ToString());
            writer.Write('\n');
        }
    }
}