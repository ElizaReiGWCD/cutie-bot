using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace discordbot.Messages.Processors
{
    public interface IMessageProcessor
    {
        string ProcessorName {get;}

        int Priority {get;}
        Task ProcessMessage(DiscordMessage discordMessage);

        bool ShouldProcess(DiscordMessage discordMessage);

        bool ShouldBreak(DiscordMessage discordMessage);
    }
}