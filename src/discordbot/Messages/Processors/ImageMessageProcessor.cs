using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using discordbot.Messages.Repository;
using discordbot.Metrics;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace discordbot.Messages.Processors
{
    public class ImageMessageProcessor : AbstractDiscordMessageProcessor
    {
        private readonly MessageRepository messageRepository;

        private Regex dtlRegex { get; } = new Regex(@" \(-?\d\)$");

        public ImageMessageProcessor(DiscordClient discordClient, CloudWatchMetrics metrics, MessageRepository messageRepository, ILogger<AbstractDiscordMessageProcessor> logger) : base(discordClient, metrics, logger)
        {
            this.messageRepository = messageRepository;
        }

        public override string ProcessorName => "ImageMessageProcessor";

        public override int Priority => 99;

        public override bool ShouldProcess(DiscordMessage discordMessage)
        {
            var words = discordMessage.Content.Split(' ');
            var containsUrl = words.Any(w => Uri.IsWellFormedUriString(w, UriKind.Absolute));

            if ((discordMessage.Attachments.Any() || containsUrl) && discordMessage.Channel.Type != ChannelType.Private)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        protected override async Task<bool> HandleMessage(DiscordMessage discordMessage)
        {
            try
            {
                int dtl = DetermineDaysToLive(discordMessage);
                ulong ttl = dtl < 0 ? 0 : (ulong)(DateTime.UtcNow.AddDays(dtl) - new DateTime(1970, 1, 1)).TotalSeconds;

                await messageRepository.StoreMessage(discordMessage, ttl);

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return false;
            }
        }

        private int DetermineDaysToLive(DiscordMessage message)
        {
            var match = dtlRegex.Match(message.Content);

            if (match.Success)
            {
                string dtl = match.Value.Trim(')', '(', ' ');
                return int.Parse(dtl);
            }
            else if (message.Attachments.Any())
            {
                return 7;
            }
            else
            {
                return 30;
            }
        }
    }
}