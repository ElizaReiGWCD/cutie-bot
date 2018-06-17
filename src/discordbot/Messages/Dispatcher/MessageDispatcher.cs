using System.Collections.Generic;
using discordbot.Metrics;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using discordbot.Messages.Processors;

namespace discordbot.Messages.Dispatcher
{

    public class MessageDispatcher
    {
        private readonly DiscordClient discordClient;
        private readonly IEnumerable<IMessageProcessor> processors;
        private readonly CloudWatchMetrics metrics;
        private readonly ILogger<MessageDispatcher> logger;

        public MessageDispatcher(DiscordClient discordClient, IEnumerable<IMessageProcessor> processors, CloudWatchMetrics metrics, ILogger<MessageDispatcher> logger)
        {
            this.discordClient = discordClient;
            this.processors = processors.OrderBy(p => p.Priority);
            this.metrics = metrics;
            this.logger = logger;
        }

        public async Task Dispatch(DiscordMessage discordMessage)
        {
            var processor = processors.FirstOrDefault(p => p.ShouldProcess(discordMessage));

            if(processor != null)
            {
                await processor.ProcessMessage(discordMessage);
            }
            else
            {
                logger.LogDebug($"No handler for message with id {discordMessage.Id}");
            }
        }
    }
}