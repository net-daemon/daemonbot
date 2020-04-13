using Algolia.Search.Clients;
using Algolia.Search.Http;
using DSharpPlus;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace bot
{

    public class Program
    {
        static SearchBot bot = new SearchBot();

        public static async Task Main(string[] args)
        {
            var discordClient = new DiscordClient(new DiscordConfiguration
            {
                Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN"),
                TokenType = TokenType.Bot
            });

            discordClient.MessageCreated += OnMessageCreated;
            await discordClient.ConnectAsync();

            CreateHostBuilder(args).Build().Run();
            await Task.Delay(-1);
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
        });

        private static async Task OnMessageCreated(MessageCreateEventArgs e)
        {

            if (Bot.IsBotUser(e))
                return; // Ignore all botusers

            if (await Bot.HandleHelp(e))
                return;

            if (await Bot.HandleCommandsPeopleMightWrite(e))
                return;

            if (await Bot.HandleSupportQueries(e, bot))
                return;

            if (Bot.IsBotUserMentioned(e) || Bot.IsBotChannel(e))
            {
                await e.Message.RespondAsync("I am sorry I could not understand your command, type command **help** for valid commands");
            }
        }
    }
}
