using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class BotService : BackgroundService
{
    private readonly ILogger<BotService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    static AlgoliaSearchClient _bot = new AlgoliaSearchClient();
    private readonly DiscordClient _client;
    public BotService(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<BotService>();
        _loggerFactory = loggerFactory;

        _logger.LogInformation("Initialize BotService..");
        _client = new DiscordClient(new DiscordConfiguration
        {
            Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN"),
            TokenType = TokenType.Bot
        });

        _client.MessageCreated += OnMessageCreated;

    }

    private async Task OnMessageCreated(MessageCreateEventArgs e)
    {
        if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Trace))
        {
            _logger.LogTrace(@"Message from {author}");
        }


        if (Bot.IsBotUser(e))
            return; // Ignore all botusers

        if (Bot.IsBotUserMentioned(e) == false && Bot.IsBotChannel(e) == false)
            return;

        if (await Bot.HandleHelp(e))
            return;

        if (await Bot.HandleSupportQueries(e, _bot))
            return;

        if (await Bot.HandleCommandsPeopleMightWrite(e))
            return;

        if (Bot.IsBotUserMentioned(e) || Bot.IsBotChannel(e))
        {
            await e.Message.RespondAsync("I am sorry I could not understand your command, type command **help** for valid commands");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Connecting to Discord..");
            await _client.ConnectAsync();

            _logger.LogInformation("Bot is running and receiving messages!");

            await Task.Delay(-1, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Ignore, normal shutdown
        }
        catch (System.Exception e)
        {
            _logger.LogError(e, "Unhandled error in BotService, exiting..");
            return;
        }

    }
}