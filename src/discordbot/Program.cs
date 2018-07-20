using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using System.Linq;
using System.Collections;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AWS.Logger.AspNetCore;
using AWS.Logger;
using Amazon.SQS;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.CloudWatch;
using discordbot.Metrics;
using discordbot.Messages.Repository;
using discordbot.Messages.Processors;
using discordbot.Messages.Dispatcher;

namespace discordbot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            RegionEndpoint awsRegion = RegionEndpoint.EUWest1;

            AWSLoggerConfig loggerConfig = new AWSLoggerConfig()
            {
                Region = awsRegion.SystemName,
                LogGroup = "CutieBotLogs"
            };

            var host = new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddLogging();

                    // Add infrastructure and clients
                    services.AddSingleton<AmazonCloudWatchClient>(new AmazonCloudWatchClient(awsRegion));
                    services.AddSingleton<AmazonSQSClient>(new AmazonSQSClient(awsRegion));
                    services.AddSingleton<AmazonDynamoDBClient>(new AmazonDynamoDBClient(awsRegion));
                    services.AddSingleton<CloudWatchMetrics>();
                    services.AddSingleton<MessageRepository>();
                    services.AddSingleton<MemberRepository>();
                    services.AddSingleton<DiscordClient>(new DiscordClient(new DiscordConfiguration
                    {
                        Token = (string)Environment.GetEnvironmentVariables()["DISCORD_TOKEN"],
                        TokenType = TokenType.Bot,
                        LogLevel = DSharpPlus.LogLevel.Debug
                    }));

                    // Add message processors
                    services.AddSingleton<IMessageProcessor, GalleryMessageProcessor>();
                    services.AddSingleton<IMessageProcessor, ImageMessageProcessor>();
                    services.AddSingleton<IMessageProcessor, MessageCountProcessor>();
                    services.AddSingleton<IMessageProcessor, MessageCountInfoProcessor>();
                    services.AddSingleton<MessageDispatcher>();

                    // Add services
                    services.AddHostedService<DiscordClientService>();
                    services.AddHostedService<SqsListenerService>();
                })
                .ConfigureLogging((context, logger) =>
                {
                    logger.AddProvider(new AWSLoggerProvider(loggerConfig));
                    logger.AddConsole();
                })
                .Build();

            using (host)
            {
                await host.StartAsync();

                await host.WaitForShutdownAsync();
            }
        }
    }
}
