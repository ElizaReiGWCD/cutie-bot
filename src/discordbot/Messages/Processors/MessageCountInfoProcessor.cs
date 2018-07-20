using System;
using System.Threading.Tasks;
using System.Linq;
using discordbot.Messages.Repository;
using discordbot.Metrics;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace discordbot.Messages.Processors
{
    public class MessageCountInfoProcessor : AbstractDiscordMessageProcessor
    {
        private readonly DiscordClient discordClient;
        private readonly MemberRepository memberRepository;
        private readonly CloudWatchMetrics metrics;
        private readonly ILogger<MessageCountInfoProcessor> logger;

        public MessageCountInfoProcessor(DiscordClient discordClient, MemberRepository memberRepository, CloudWatchMetrics metrics, ILogger<MessageCountInfoProcessor> logger) : base(discordClient, metrics, logger)
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
            return true;
        }

        public override bool ShouldProcess(DiscordMessage discordMessage)
        {
            return !string.IsNullOrEmpty(discordMessage.Content) 
                   && discordMessage.Content.StartsWith(";;messagecount")
                   && IsMod(discordMessage);
        }

        protected override async Task<bool> HandleMessage(DiscordMessage discordMessage)
        {
            logger.LogInformation($"Processing message {discordMessage.Content}");

            var message = discordMessage.Content.Split(' ', 2);

            if(message.Length < 2) {
                await discordMessage.Channel.SendMessageAsync("Please provide a username");
                return true;
            }

            var member = await memberRepository.RetrieveMember(message[1]);

            if(member == null)
            {
                await discordMessage.Channel.SendMessageAsync($"User {message[1]} doesn't exist");
                return true;
            }
            else
            {
                await discordMessage.Channel.SendMessageAsync($"User {member.Username} has been in this channel since {member.JoinedAt}, the first message was on {member.FirstMessage}, posted {member.NoOfMessages} messages in total, and the last message was on {member.LastEdited}");
                return true;
            }
        }
    }
}