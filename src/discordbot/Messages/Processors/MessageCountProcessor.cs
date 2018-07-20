using System;
using System.Threading.Tasks;
using discordbot.Messages.Repository;
using discordbot.Metrics;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace discordbot.Messages.Processors
{
    public class MessageCountProcessor : AbstractDiscordMessageProcessor
    {
        private readonly DiscordClient discordClient;
        private readonly MemberRepository memberRepository;
        private readonly CloudWatchMetrics metrics;
        private readonly ILogger<AbstractDiscordMessageProcessor> logger;

        public MessageCountProcessor(DiscordClient discordClient, MemberRepository memberRepository, CloudWatchMetrics metrics, ILogger<MessageCountProcessor> logger) : base(discordClient, metrics, logger)
        {
            this.discordClient = discordClient;
            this.memberRepository = memberRepository;
            this.metrics = metrics;
            this.logger = logger;
        }

        public override string ProcessorName => "MessageCountProcessor";

        public override int Priority => 0;

        public override bool ShouldBreak(DiscordMessage discordMessage)
        {
            return false;
        }

        public override bool ShouldProcess(DiscordMessage discordMessage)
        {
            return true;
        }

        protected override async Task<bool> HandleMessage(DiscordMessage discordMessage)
        {
            try {
                var member = await memberRepository.SaveMember(discordMessage);

                if(member.NoOfMessages == 30)
                {
                    await discordClient.GetChannelAsync(453269452906037258).Result.SendMessageAsync($"Member {member.Username} has now more than 30 messages, please check if they are a perv");
                }

                return true;
            } catch(Exception ex)
            {
                logger.LogError($"Could not update member {discordMessage.Author.Username} because of {ex.Message}");
                return false;
            }
        }
    }
}