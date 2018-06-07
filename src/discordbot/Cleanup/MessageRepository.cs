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

namespace discordbot
{
    public class MessageRepository
    {
        private AmazonDynamoDBClient dbClient {get;set;}

        private DynamoDBContext dbContext {get;set;}

        private Regex dtlRegex {get;} = new Regex(@" \(-?\d\)$");

        public MessageRepository()
        {
            dbClient = new AmazonDynamoDBClient(RegionEndpoint.EUWest1);
            dbContext = new DynamoDBContext(dbClient, new DynamoDBContextConfig(){
                ConsistentRead = true,
                IgnoreNullValues = false
            });
        }

        public void Initialize()
        {
            var table = dbContext.GetTargetTable<CleanupMessage>();
            Console.WriteLine($"Found cleanup table: {table.TableName}");
        }

        public async Task StoreMessage(DiscordMessage message)
        {
            if(!ShouldSave(message))
                return;

            int dtl = DetermineDaysToLive(message);
            ulong ttl = dtl < 0 ? 0 : (ulong)(DateTime.UtcNow.AddDays(dtl) - new DateTime(1970, 1, 1)).TotalSeconds;

            Console.WriteLine($"Saving message: {message} for {dtl} days, with ttl {ttl}");

            var cleanupMessage = new CleanupMessage();
            cleanupMessage.MessageCleanupTableId = message.Id.ToString();
            cleanupMessage.ChannelId = message.ChannelId;
            cleanupMessage.InsertedAt = DateTime.Now;
            cleanupMessage.LastEdited = DateTime.Now;
            cleanupMessage.TTL = ttl;

            await dbContext.SaveAsync(cleanupMessage);

            Console.WriteLine($"Saved message with ID {message.Id}");
        }

        private bool ShouldSave(DiscordMessage message)
        {
            // var serializer = JsonSerializer.CreateDefault();
            // var writer = new StringWriter();
            // serializer.Serialize(writer, message);
            // Console.WriteLine(writer.ToString());

            var words = message.Content.Split(' ');
            var containsUrl = words.Any(w => Uri.IsWellFormedUriString(w, UriKind.Absolute));

            if(message.Attachments.Any() || containsUrl && !message.Author.IsBot)
            {
                Console.WriteLine(message);
                return true;
            }
            else
            {
                return false;
            }
        }

        private int DetermineDaysToLive(DiscordMessage message)
        {
            var match = dtlRegex.Match(message.Content);

            if(match.Success)
            {
                string dtl = match.Value.Trim(')', '(', ' ');
                return int.Parse(dtl);
            }
            else if(message.Attachments.Any())
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