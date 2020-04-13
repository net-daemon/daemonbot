using Algolia.Search.Clients;
using Algolia.Search.Http;
using DSharpPlus;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            await Task.Delay(-1);
        }

        private static async Task OnMessageCreated(MessageCreateEventArgs e)
        {
            System.Console.WriteLine($"Channel: {e.Message.Channel.Name} ({e.Message.Channel.Id}) ");
            System.Console.WriteLine($"User: {e.Message.Author.Username}");
            System.Console.WriteLine($"Content: \r\n{e.Message.Content}");
            if (await Bot.HandleSupportQueries(e, bot))
                return;

            if (e.Message.MentionedRoles.Select(n => n.Id == 699226092426231858).Count() > 0 ||
                e.Message.MentionedUsers.Select(n => n.Id == 699223277683343361).Count() > 0)
            {
                await e.Message.RespondAsync("I am sorry I could not understand your command, say **help** for commands");
            }
            // if (string.Equals(e.Message.Content, "hello", StringComparison.OrdinalIgnoreCase))
            // {
            //     await e.Message.RespondAsync(e.Message.Author.Username);
            // }
        }
        // public static void Main(string[] args)
        // {
        //     CreateHostBuilder(args).Build().Run();
        // }

        // public static IHostBuilder CreateHostBuilder(string[] args) =>
        //     Host.CreateDefaultBuilder(args)
        //         .ConfigureWebHostDefaults(webBuilder =>
        //         {
        //             webBuilder.UseStartup<Startup>();
        //         });
    }
}
