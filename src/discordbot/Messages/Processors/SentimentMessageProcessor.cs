using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using discordbot.Messages.Processors;
using discordbot.Metrics;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using Amazon.Comprehend;
using Amazon.Comprehend.Model;
using System;

public class SentimentMessageProcessor : AbstractDiscordMessageProcessor
{
    public SentimentMessageProcessor(DiscordClient discordClient,
                                     CloudWatchMetrics metrics,
                                     ILogger<AbstractDiscordMessageProcessor> logger,
                                     AmazonComprehendClient amazonComprehendClient) : base(discordClient, metrics, logger)
    {
        AmazonComprehendClient = amazonComprehendClient;
    }

    public override string ProcessorName => "SentimentMessageProcessor";

    public override int Priority => 999;

    public AmazonComprehendClient AmazonComprehendClient { get; }

    public override bool ShouldBreak(DiscordMessage discordMessage)
    {
        return false;
    }

    public override bool ShouldProcess(DiscordMessage discordMessage)
    {
        return discordMessage.Channel.Name == "general";
    }

    protected override async Task<bool> HandleMessage(DiscordMessage discordMessage)
    {
        logger.LogInformation($"Getting 10 messages before {discordMessage.Id}");
        IReadOnlyList<DiscordMessage> messages = await discordMessage.Channel.GetMessagesAsync(10, before: discordMessage.Id);
        logger.LogInformation($"Got {messages.Count} messages before {discordMessage.Id}");

        string text = messages
            .Select(m => m.Content)
            .Aggregate("", (acc, value) => acc + "\n" + value);

        DetectSentimentRequest detectSentimentRequest = new DetectSentimentRequest();
        detectSentimentRequest.LanguageCode = "en";
        detectSentimentRequest.Text = text;

        logger.LogInformation($"Calling AWS::Comprehend with {messages.Count} messages and total text length {text.Length}");
        DetectSentimentResponse detectSentimentResponse = await AmazonComprehendClient.DetectSentimentAsync(detectSentimentRequest);
        logger.LogInformation($"Called AWS::Comprehend with {messages.Count} messages");

        if(detectSentimentResponse.HttpStatusCode != System.Net.HttpStatusCode.OK) {
            return false;
        }

        await metrics.AddCounter("Discord.SentimentScore.Positive", detectSentimentResponse.SentimentScore.Positive);
        await metrics.AddCounter("Discord.SentimentScore.Negative", detectSentimentResponse.SentimentScore.Negative);
        await metrics.AddCounter("Discord.SentimentScore.Mixed", detectSentimentResponse.SentimentScore.Mixed);
        await metrics.AddCounter("Discord.SentimentScore.Neutral", detectSentimentResponse.SentimentScore.Neutral);
        await metrics.AddCounter($"Discord.Sentiment.{detectSentimentResponse.Sentiment.Value}", 1);

        return true;
    }
}