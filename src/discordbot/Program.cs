﻿using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using System.Linq;
using System.Collections;

namespace discordbot
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            var discord = new DiscordClient(new DiscordConfiguration
            {
                Token = (string)Environment.GetEnvironmentVariables()["DISCORD_TOKEN"],
                TokenType = TokenType.Bot,
                UseInternalLogHandler = true,
                LogLevel = LogLevel.Debug
            });

            var commands = discord.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefix = ";;"
            });

            commands.RegisterCommands<CutieCommands>();

            await discord.ConnectAsync();
            await Task.Delay(-1); //wait so the program doesn't quit immediately
        }
    }
}