using System;
using Amazon.DynamoDBv2.DataModel;

namespace discordbot.Messages.Repository.Model
{
    [DynamoDBTable("MessageCleanupTable")]
    public class CleanupMessage
    {
        [DynamoDBHashKey]
        public string MessageCleanupTableId {get;set;}

        public ulong ChannelId {get;set;}

        public DateTime InsertedAt {get;set;}

        public DateTime LastEdited {get;set;}

        public ulong TTL {get;set;}
    }
}