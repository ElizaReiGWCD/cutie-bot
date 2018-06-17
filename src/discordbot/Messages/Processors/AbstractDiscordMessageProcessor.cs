using System;
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
            logger.LogDebug($"MessageProcessor {ProcessorName} received message with id {discordMessage.Id}");

            logger.LogDebug($"MessageProcessor {ProcessorName} is handling message with id {discordMessage.Id}");
            var isSuccess = await HandleMessage(discordMessage);

            if (isSuccess)
            {
                await metrics.AddCounter($"Discord.{ProcessorName}.Success", 1);
                logger.LogDebug($"MessageProcessor {ProcessorName} succesfully handled message with id {discordMessage.Id}");
            }
            else
            {
                await metrics.AddCounter($"Discord.{ProcessorName}.Error", 1);
                logger.LogDebug($"MessageProcessor {ProcessorName} did not successfully handle message with id {discordMessage.Id}");
            }

        }

        public abstract bool ShouldProcess(DiscordMessage discordMessage);
        protected abstract Task<bool> HandleMessage(DiscordMessage discordMessage);
    }
}