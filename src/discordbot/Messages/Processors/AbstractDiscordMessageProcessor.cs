using System;
using System.Linq;
using System.Threading.Tasks;
using discordbot.Metrics;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace discordbot.Messages.Processors
{
    public abstract class AbstractDiscordMessageProcessor : IMessageProcessor
    {
        protected readonly DiscordClient discordClient;
        protected readonly CloudWatchMetrics metrics;
        protected readonly ILogger<AbstractDiscordMessageProcessor> logger;

        public abstract string ProcessorName { get; }

        public abstract int Priority {get;}
        
        public AbstractDiscordMessageProcessor(DiscordClient discordClient,
                                               CloudWatchMetrics metrics,
                                               ILogger<AbstractDiscordMessageProcessor> logger)
        {
            this.discordClient = discordClient;
            this.metrics = metrics;
            this.logger = logger;
        }

        public async Task ProcessMessage(DiscordMessage discordMessage)
        {
            logger.LogInformation($"MessageProcessor {ProcessorName} received message with id {discordMessage.Id}");

            logger.LogInformation($"MessageProcessor {ProcessorName} is handling message with id {discordMessage.Id}");
            var isSuccess = await HandleMessage(discordMessage);

            if (isSuccess)
            {
                await metrics.AddCounter($"Discord.{ProcessorName}.Success", 1);
                logger.LogInformation($"MessageProcessor {ProcessorName} succesfully handled message with id {discordMessage.Id}");
            }
            else
            {
                await metrics.AddCounter($"Discord.{ProcessorName}.Error", 1);
                logger.LogInformation($"MessageProcessor {ProcessorName} did not successfully handle message with id {discordMessage.Id}");
            }

        }

        protected bool IsPerv(DiscordMessage discordMessage)
        {
            return HasRole(discordMessage, "Perv", "Ultra Super Perv", "Taylor Swift");
        }

        protected bool IsMod(DiscordMessage discordMessage)
        {
            return HasRole(discordMessage, "Ultra Super Perv", "Taylor Swift");
        }

        protected bool HasRole(DiscordMessage discordMessage, params string[] roleNames)
        {
            var member = discordMessage.Channel.Guild.Members.FirstOrDefault(m => m.Id == discordMessage.Author.Id);

            if(member == null)
            {
                logger.LogInformation($"Author with id {discordMessage.Author.Id} is not in the guild");
                return false;
            }
            
            return member.Roles.Any(r => roleNames.Contains(r.Name));
        }

        public abstract bool ShouldProcess(DiscordMessage discordMessage);
        protected abstract Task<bool> HandleMessage(DiscordMessage discordMessage);
        public abstract bool ShouldBreak(DiscordMessage discordMessage);
    }
}