using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using discordbot.Metrics;
using discordbot.Messages.Dispatcher;

namespace discordbot
{
    public class DiscordClientService : IHostedService
    {
        private readonly DiscordClient discordClient;
        private readonly MessageDispatcher dispatcher;
        private readonly CloudWatchMetrics metrics;
        private readonly ILogger<DiscordClientService> logger;

        public DiscordClientService(DiscordClient discordClient,
                                    MessageDispatcher dispatcher,
                                    CloudWatchMetrics metrics,
                                    ILogger<DiscordClientService> logger)
        {
            this.discordClient = discordClient;
            this.dispatcher = dispatcher;
            this.metrics = metrics;
            this.logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting the DiscordClient that will receive events");

            discordClient.MessageCreated += async (evt) =>
            {
                await metrics.AddCounter("Discord.MessagesReceived", 1);
                await dispatcher.Dispatch(evt.Message);
            };

            await discordClient.ConnectAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await discordClient.DisconnectAsync();
        }
    }
}