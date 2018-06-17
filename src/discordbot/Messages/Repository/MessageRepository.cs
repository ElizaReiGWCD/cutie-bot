using System;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using DSharpPlus.Entities;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using discordbot.Messages.Repository.Model;

namespace discordbot.Messages.Repository
{
    public class MessageRepository
    {
        private readonly ILogger<MessageRepository> logger;

        private AmazonDynamoDBClient dbClient {get;set;}

        private DynamoDBContext dbContext {get;set;}

        private Regex dtlRegex {get;} = new Regex(@" \(-?\d\)$");

        public MessageRepository(AmazonDynamoDBClient dbClient, ILogger<MessageRepository> logger)
        {
            this.dbClient = dbClient;
            this.logger = logger;
            dbContext = new DynamoDBContext(dbClient, new DynamoDBContextConfig(){
                ConsistentRead = true,
                IgnoreNullValues = false
            });
        }

        public void Initialize()
        {
            var table = dbContext.GetTargetTable<CleanupMessage>();
            logger.LogInformation($"Found cleanup table: {table.TableName}");
        }

        public async Task StoreMessage(DiscordMessage message, ulong ttl)
        {

            logger.LogInformation($"Saving message: {message} with ttl {ttl}");

            var cleanupMessage = new CleanupMessage();
            cleanupMessage.MessageCleanupTableId = message.Id.ToString();
            cleanupMessage.ChannelId = message.ChannelId;
            cleanupMessage.InsertedAt = DateTime.Now;
            cleanupMessage.LastEdited = DateTime.Now;
            cleanupMessage.TTL = ttl;

            await dbContext.SaveAsync(cleanupMessage);

            logger.LogInformation($"Saved message with ID {message.Id}");
        }
    }
}