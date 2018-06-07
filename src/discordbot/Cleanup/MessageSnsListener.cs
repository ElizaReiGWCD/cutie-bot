using System;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using Amazon;
using Amazon.DynamoDBv2.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using DSharpPlus;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace discordbot
{
    public class MessageSnsListener
    {
        private AmazonSQSClient SqsClient { get; set; }

        private string QueueUrl { get; set; }
        private DiscordClient DiscordClient { get; }

        private Timer timer { get; set; }

        public MessageSnsListener(DiscordClient client)
        {
            SqsClient = new AmazonSQSClient(RegionEndpoint.EUWest1);
            DiscordClient = client;
        }

        public async Task Initialize()
        {
            QueueUrl = (await SqsClient.GetQueueUrlAsync("MessageCleanupQueue").ConfigureAwait(false)).QueueUrl;
            Console.WriteLine($"Succesfully got Queue {QueueUrl}");

            timer = new Timer(TimeSpan.FromSeconds(10).TotalMilliseconds);
            timer.AutoReset = true;
            timer.Elapsed += Poll;
            timer.Start();
        }

        public async void Poll(object sender, ElapsedEventArgs evt)
        {
            DiscordClient.DebugLogger.LogMessage(LogLevel.Debug, "Cleanup", "Polling for messages", DateTime.UtcNow);
            ReceiveMessageResponse messages = await ReceiveMessages();
            DiscordClient.DebugLogger.LogMessage(LogLevel.Debug, "Cleanup", $"Got {messages.Messages.Count} messages", DateTime.UtcNow);

            if (messages.Messages.Count > 0)
            {
                var serializer = JsonSerializer.CreateDefault(new JsonSerializerSettings()
                {
                    Error = (s, errorArgs) =>
                    {
                        var currentError = errorArgs.ErrorContext.Error.Message;
                        errorArgs.ErrorContext.Handled = true;
                    }
                });

                var logChannel = await DiscordClient.GetChannelAsync(454044180923285504);

                foreach (var message in messages.Messages)
                {
                    var queueMessage = JObject.Parse(message.Body);

                    var dynamoMessage = queueMessage["Message"].ToString();

                    DiscordClient.DebugLogger.LogMessage(LogLevel.Debug, "Cleanup", dynamoMessage, DateTime.UtcNow);

                    var dynamoEvent = serializer.Deserialize<Record>(new JsonTextReader(new StringReader(dynamoMessage)));

                    if (dynamoEvent.EventName.Value == "REMOVE")
                    {
                        var cleanupMessage = dynamoEvent.Dynamodb.OldImage;
                        var channelId = ulong.Parse(cleanupMessage["ChannelId"].N);
                        var messageId = ulong.Parse(cleanupMessage["MessageCleanupTableId"].S);

                        var channel = await DiscordClient.GetChannelAsync(channelId);
                        var discordMessage = await channel.GetMessageAsync(messageId);

                        await logChannel.SendMessageAsync($"Removed message '{discordMessage}'");

                        await channel.DeleteMessageAsync(discordMessage, "expired link");
                    }
                }

                await DeleteMessages(messages);
            }
        }

        private async Task DeleteMessages(ReceiveMessageResponse messages)
        {
            var deleteMessageRequest = new DeleteMessageBatchRequest();
            deleteMessageRequest.QueueUrl = QueueUrl;
            deleteMessageRequest.Entries = messages.Messages.Select(((m, i) =>
            {
                return new DeleteMessageBatchRequestEntry(i.ToString(), m.ReceiptHandle);
            })).ToList();
            await SqsClient.DeleteMessageBatchAsync(deleteMessageRequest);
        }

        private async Task<ReceiveMessageResponse> ReceiveMessages()
        {
            var receiveMessageRequest = new ReceiveMessageRequest();
            receiveMessageRequest.QueueUrl = QueueUrl;
            receiveMessageRequest.MaxNumberOfMessages = 10;
            receiveMessageRequest.WaitTimeSeconds = 0;

            var messages = await SqsClient.ReceiveMessageAsync(receiveMessageRequest);
            return messages;
        }
    }
}