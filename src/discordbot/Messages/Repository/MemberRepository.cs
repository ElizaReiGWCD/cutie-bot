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
using Amazon.DynamoDBv2.Model;

namespace discordbot.Messages.Repository
{
    public class MemberRepository
    {
        private readonly ILogger<MemberRepository> logger;

        private AmazonDynamoDBClient dbClient {get;set;}

        private DynamoDBContext dbContext {get;set;}

        private Regex dtlRegex {get;} = new Regex(@" \(-?\d\)$");

        public MemberRepository(AmazonDynamoDBClient dbClient, ILogger<MemberRepository> logger)
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
            var table = dbContext.GetTargetTable<Member>();
            logger.LogInformation($"Found member table: {table.TableName}");
        }

        public async Task<Member> RetrieveMember(string username)
        {
            try 
            {
                logger.LogInformation($"Retrieving member with username {username}");
                return await dbContext.LoadAsync<Member>(username);
            } 
            catch(ResourceNotFoundException ex) 
            {
                return null;
            }
        }

        public async Task<Member> SaveMember(DiscordMessage message)
        {
            string username = message.Author.Username;
            var member = await RetrieveMember(username);

            if(member == null)
            {
                logger.LogInformation($"Creating new entry for member {username}");
                member = new Member() 
                { 
                    Username = username,
                    NoOfMessages = 0,
                    FirstMessage = DateTime.UtcNow
                };
            }

            member.NoOfMessages += 1;
            member.LastEdited = DateTime.UtcNow;

            logger.LogInformation($"Saving member: {member.Username}");

            await dbContext.SaveAsync(member);

            logger.LogInformation($"Saved member: {member.Username}");

            return member;
        }
    }
}