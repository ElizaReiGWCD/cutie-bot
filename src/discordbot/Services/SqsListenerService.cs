using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Microsoft.Extensions.Hosting;
using DSharpPlus;
using System;
using System.Timers;
using Amazon.SQS.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Amazon.DynamoDBv2.Model;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using DSharpPlus.Entities;
using discordbot.Metrics;
using DSharpPlus.Exceptions;

namespace discordbot
{
    public class SqsListenerService : IHostedService
    {
        private System.Timers.Timer timer;
        private DiscordChannel LogChannel;
        private readonly DiscordClient discordClient;
        private readonly AmazonSQSClient sqsClient;
        private readonly CloudWatchMetrics metrics;
        private readonly ILogger<SqsListenerService> logger;

        private JsonSerializer Serializer { get; }
        public string QueueUrl { get; private set; }

        public SqsListenerService(DiscordClient discordClient,
                                  AmazonSQSClient sqsClient,
                                  CloudWatchMetrics metrics,
                                  ILogger<SqsListenerService> logger)
        {
            Serializer = JsonSerializer.CreateDefault(new JsonSerializerSettings()
                {
                    Error = (s, errorArgs) =>
                    {
                        var currentError = errorArgs.ErrorContext.Error.Message;
                        errorArgs.ErrorContext.Handled = true;
                    }
                });
            this.discordClient = discordClient;
            this.sqsClient = sqsClient;
            this.metrics = metrics;
            this.logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting queue listener");

            QueueUrl = (await sqsClient.GetQueueUrlAsync("MessageCleanupQueue").ConfigureAwait(false)).QueueUrl;

            logger.LogInformation($"Succesfully got Queue {QueueUrl}");

            timer = new System.Timers.Timer(TimeSpan.FromSeconds(60).TotalMilliseconds);
            timer.AutoReset = true;
            timer.Elapsed += Poll;
            timer.Start();

            logger.LogInformation($"Successfully started SQS Listener service for {QueueUrl}");

            logger.LogInformation($"Getting the channel to log events to");
            LogChannel = await discordClient.GetChannelAsync(454044180923285504);
            logger.LogInformation($"Logging events to channel {LogChannel.Name}");
        }

        public async void Poll(object sender, ElapsedEventArgs evt)
        {
            logger.LogInformation($"Polling for messages from queue {QueueUrl}");

            ReceiveMessageResponse messages = await ReceiveMessages();

            logger.LogInformation($"Got {messages.Messages.Count} messages from queue {QueueUrl}");
            await metrics.AddCounter("SQS.MessagesReceived", messages.Messages.Count);

            if (messages.Messages.Count > 0)
            {
                foreach (var message in messages.Messages)
                {
                    var queueMessage = JObject.Parse(message.Body);

                    var dynamoMessage = queueMessage["Message"].ToString();

                    logger.LogDebug(dynamoMessage);

                    var dynamoEvent = Serializer.Deserialize<Record>(new JsonTextReader(new StringReader(dynamoMessage)));

                    if (dynamoEvent.EventName.Value == "REMOVE")
                    {
                        var cleanupMessage = dynamoEvent.Dynamodb.OldImage;
                        var channelId = ulong.Parse(cleanupMessage["ChannelId"].N);
                        var messageId = ulong.Parse(cleanupMessage["MessageCleanupTableId"].S);

                        try {
                            var channel = await discordClient.GetChannelAsync(channelId);
                            var discordMessage = await channel.GetMessageAsync(messageId);

                            await channel.DeleteMessageAsync(discordMessage, "expired link");
                            logger.LogInformation($"Deleted discord message with id {messageId} from channel {channelId}");
                            await LogChannel.SendMessageAsync($"Removed message '{messageId}'");
                        }
                        catch(NotFoundException ex)
                        {
                            logger.LogWarning($"Tried to delete message {messageId} but it was not found and probably already deleted");
                        }
                        catch(UnauthorizedException ex)
                        {
                            logger.LogError($"Could not delete message {messageId} in channel {channelId} because of lacking permissions");
                        }
                    }

                    await DeleteMessage(message);
                    logger.LogInformation($"Successfully deleted message with id {message.ReceiptHandle} from queue");
                }
            }
        }

        private async Task DeleteMessage(Message message)
        {
            var deleteMessageRequest = new DeleteMessageRequest();
            deleteMessageRequest.QueueUrl = QueueUrl;
            deleteMessageRequest.ReceiptHandle = message.ReceiptHandle;
            await sqsClient.DeleteMessageAsync(deleteMessageRequest);
        }

        private async Task<ReceiveMessageResponse> ReceiveMessages()
        {
            var receiveMessageRequest = new ReceiveMessageRequest();
            receiveMessageRequest.QueueUrl = QueueUrl;
            receiveMessageRequest.MaxNumberOfMessages = 10;
            receiveMessageRequest.WaitTimeSeconds = 0;

            var messages = await sqsClient.ReceiveMessageAsync(receiveMessageRequest);
            return messages;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            timer.Stop();
            timer.Dispose();
            return Task.CompletedTask;
        }
    }
}