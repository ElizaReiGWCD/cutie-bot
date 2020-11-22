using System;
using System.Collections.Generic;
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

        private readonly List<MetricDatum> MetricData;

        public CloudWatchMetrics(AmazonCloudWatchClient cloudWatchClient, ILogger<CloudWatchMetrics> logger)
        {
            this.cloudWatchClient = cloudWatchClient;
            this.logger = logger;

            MetricData = new List<MetricDatum>();
        }

        public async Task AddCounter(string metricName, double count)
        {
            var metricDatum = new MetricDatum();
            metricDatum.MetricName = metricName;
            metricDatum.Value = count;
            metricDatum.TimestampUtc = DateTime.UtcNow;
            metricDatum.Unit = StandardUnit.Count;
            MetricData.Add(metricDatum);
            
            if(MetricData.Count > 10)
            {
                var metricData = new PutMetricDataRequest();
                metricData.MetricData = MetricData;
                metricData.Namespace = "CutieBot";

                await cloudWatchClient.PutMetricDataAsync(metricData);
                
                MetricData.Clear();
            }
            
        }
    }
}