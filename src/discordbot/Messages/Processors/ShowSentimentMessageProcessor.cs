using System.Threading.Tasks;
using discordbot.Messages.Processors;
using discordbot.Metrics;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

class ShowSentimentMessageProcessor : AbstractDiscordMessageProcessor
{
    public ShowSentimentMessageProcessor(DiscordClient discordClient,
                                         CloudWatchMetrics metrics,
                                         ILogger<AbstractDiscordMessageProcessor> logger,
                                         AmazonCloudWatchClient cloudWatchClient) : base(discordClient, metrics, logger)
    {
        CloudWatchClient = cloudWatchClient;
    }

    public override string ProcessorName => "ShowSentimentMessageProcessor";

    public override int Priority => 99;

    public AmazonCloudWatchClient CloudWatchClient { get; }

    public override bool ShouldBreak(DiscordMessage discordMessage)
    {
        return true;
    }

    public override bool ShouldProcess(DiscordMessage discordMessage)
    {
        return !string.IsNullOrEmpty(discordMessage.Content)
                   && discordMessage.Content.StartsWith(";;sentiment")
                   && IsMod(discordMessage);
    }

    protected override async Task<bool> HandleMessage(DiscordMessage discordMessage)
    {
        GetMetricWidgetImageRequest request = new GetMetricWidgetImageRequest();
        request.OutputFormat = "png";
        request.MetricWidget = @"
            {
                ""metrics"": [
                    [ ""CutieBot"", ""Discord.SentimentScore.Positive"", { ""label"": ""[avg: ${AVG}] Discord.SentimentScore.Positive"", ""color"": ""#2ca02c"" } ],
                    [ ""."", ""Discord.SentimentScore.Neutral"", { ""label"": ""[avg: ${AVG}] Discord.SentimentScore.Neutral"", ""color"": ""#f4f116"" } ],
                    [ ""."", ""Discord.SentimentScore.Negative"", { ""label"": ""[avg: ${AVG}] Discord.SentimentScore.Negative"", ""color"": ""#d62728"" } ],
                    [ ""."", ""Discord.SentimentScore.Mixed"", { ""label"": ""[avg: ${AVG}] Discord.SentimentScore.Mixed"", ""color"": ""#1f77b4"" } ]
                ],
                ""view"": ""pie"",
                ""stacked"": false,
                ""stat"": ""Average"",
                ""period"": 300,
                ""setPeriodToTimeRange"": true,
                ""width"": 500,
                ""height"": 500,
                ""start"": ""-P1D"",
                ""end"": ""P0D""
            }
        ";
        GetMetricWidgetImageResponse getMetricWidgetImageResponse = await CloudWatchClient.GetMetricWidgetImageAsync(request);

        if(getMetricWidgetImageResponse.HttpStatusCode != System.Net.HttpStatusCode.OK) {
            return false;
        }

        await discordMessage.Channel.SendFileAsync(
            content: $"Here's the sentiment of the {discordMessage.Channel.Name} channel",
            file_data: getMetricWidgetImageResponse.MetricWidgetImage,
            file_name: "MetricWidget.png"
        );

        return true;
    }
}