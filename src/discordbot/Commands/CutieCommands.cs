using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Linq;
using DSharpPlus;
using System;

namespace discordbot
{
    public class CutieCommands
    {
        [Command("post")]
        public async Task post(CommandContext ctx, string link)
        {
            var log = ctx.Client.DebugLogger;
            log.LogMessage(LogLevel.Debug, "", $"Got post command with link {link}", DateTime.Now);

            var channel = await ctx.Client.GetChannelAsync(452949832802500628).ConfigureAwait(false);
            log.LogMessage(LogLevel.Debug, "", $"Got channel {channel.Name}", DateTime.Now);

            var messages = await channel.GetMessagesAsync();
            log.LogMessage(LogLevel.Debug, "", $"Got {messages.Count}", DateTime.Now);

            var message = messages.FirstOrDefault(m => m.MentionedUsers.Contains(ctx.Message.Author));

            if (message != null) {
                log.LogMessage(LogLevel.Debug, "", $"Found message '{message.Content}', modifying.", DateTime.Now);

                await message.ModifyAsync(content: $"{ctx.Message.Author.Mention} {link}");
                await ctx.RespondAsync($"Modified {ctx.Message.Author.Mention}'s cutie image, check it out!");
            } else {
                log.LogMessage(LogLevel.Debug, "", $"Did not find existing image, making new message", DateTime.Now);

                await channel.SendMessageAsync(content: $"{ctx.Message.Author.Mention} {link}");
                log.LogMessage(LogLevel.Debug, "", $"Posted message '{ctx.Message.Author.Mention} {link}'", DateTime.Now);

                await ctx.RespondAsync($"Added {ctx.Message.Author.Mention}'s cutie image, check it out!");
            }
        }
    }
}