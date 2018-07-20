using System;
using Amazon.DynamoDBv2.DataModel;

namespace discordbot.Messages.Repository.Model
{
    [DynamoDBTable("MemberTable")]
    public class Member
    {
        [DynamoDBHashKey]
        public string Username {get;set;}

        public ulong NoOfMessages {get;set;}

        public DateTime FirstMessage {get;set;}

        public DateTime JoinedAt {get;set;}

        public DateTime LastEdited {get;set;}
    }
}