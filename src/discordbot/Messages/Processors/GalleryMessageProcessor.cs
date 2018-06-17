using System;
using System.Linq;
using System.Threading.Tasks;
using discordbot.Metrics;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace discordbot.Messages.Processors
{
    public class GalleryMessageProcessor : AbstractDiscordMessageProcessor
    {

        public GalleryMessageProcessor(DiscordClient discordClient, CloudWatchMetrics metrics, ILogger<AbstractDiscordMessageProcessor> logger) : base(discordClient, metrics, logger)
        {}

        public override string ProcessorName => "GalleryMessageProcessor";

        public override int Priority => 10;

        public override bool ShouldProcess(DiscordMessage discordMessage)
        {
            return !string.IsNullOrEmpty(discordMessage.Content) && discordMessage.Content.StartsWith(";;post");
        }

        protected override async Task<bool> HandleMessage(DiscordMessage discordMessage)
        {
            try
            {
                var args = discordMessage.Content.Split(" ", StringSplitOptions.RemoveEmptyEntries);

                if (args.Length < 2)
                {
                    logger.LogInformation("Post command didn't have enough args");
                    return true;
                }

                var channel = await discordClient.GetChannelAsync(452949832802500628).ConfigureAwait(false);
                var messages = await channel.GetMessagesAsync().ConfigureAwait(false);

                var message = messages.FirstOrDefault(m => m.MentionedUsers.Contains(discordMessage.Author));

                if (message != null)
                {
                    logger.LogInformation($"Deleting message from gallery for user {message.Author.Username} with messageid {message.Id}");
                    await message.DeleteAsync("posting to gallery");
                }
                
                await channel.SendMessageAsync(content: $"{discordMessage.Author.Mention} {args[1]}");
                await discordMessage.Channel.SendMessageAsync($"Added {discordMessage.Author.Mention}'s cutie image, check it out!");

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return false;
            }
        }
    }
}