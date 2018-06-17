using System;
using System.Threading.Tasks;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Microsoft.Extensions.Logging;

namespace discordbot.Metrics
{
    public class CloudWatchMetrics
    {
        private readonly AmazonCloudWatchClient cloudWatchClient;
        private readonly ILogger<CloudWatchMetrics> logger;

        public CloudWatchMetrics(AmazonCloudWatchClient cloudWatchClient, ILogger<CloudWatchMetrics> logger)
        {
            this.cloudWatchClient = cloudWatchClient;
            this.logger = logger;
        }

        public async Task AddCounter(string metricName, double count)
        {
            var metricData = new PutMetricDataRequest();

            var metricDatum = new MetricDatum();
            metricDatum.MetricName = metricName;
            metricDatum.Value = count;
            metricDatum.Timestamp = DateTime.UtcNow;
            metricDatum.Unit = StandardUnit.Count;
            
            metricData.MetricData.Add(metricDatum);
            metricData.Namespace = "CutieBot";

            await cloudWatchClient.PutMetricDataAsync(metricData);
        }
    }
}