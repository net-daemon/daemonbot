using System;
using System.Globalization;
using System.Linq;
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
    private readonly IBotRunner _botRunner;
    private readonly DiscordClient _discordClient;
    private readonly ulong? _botChannel;

    /// <summary>
    ///     Hosted service implementing the lifetime of the bot
    /// </summary>
    /// <param name="loggerFactory">Logfactory used</param>
    /// <param name="runner">The botrunner used</param>
    public BotService(
        ILoggerFactory loggerFactory,
        IBotRunner runner
        )
    {
        _logger = loggerFactory.CreateLogger<BotService>();
        _loggerFactory = loggerFactory;
        _botRunner = runner;

        _logger.LogInformation("Initialize BotService..");

        // Todo: Decouple the Discord service to able to use in other chats
        _discordClient = new DiscordClient(new DiscordConfiguration
        {
            Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN"),
            TokenType = TokenType.Bot
        });

        var botChannel = Environment.GetEnvironmentVariable("DISCORD_BOTCHANNEL");

        if (ulong.TryParse(botChannel, out var channelId))
            _botChannel = channelId;

        _discordClient.MessageCreated += OnMessageCreated;

    }

    /// <summary>
    ///     Returns true if the message is to the bot
    /// </summary>
    private bool IsThisMessageForTheBot(MessageCreateEventArgs e)
    {
        // Check if this is the botchannel
        if (_botChannel is object && _botChannel == e.Message.Channel.Id)
            return true;

        // Check if the bot is mentioned
        if (e.Message.MentionedUsers.Where(n => n.IsBot == true).Count() > 0)
            return true;

        return false;
    }

    /// <summary>
    ///     Called each new message from Discord
    /// </summary>
    private async Task OnMessageCreated(MessageCreateEventArgs e)
    {
        if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Trace))
        {
            _logger.LogTrace(@"Message from {author}");
        }

        if (e.Message.Author.IsBot || IsThisMessageForTheBot(e) == false)
            return; // Ignore all botusers or messages not for the bot

        var parser = new BotParser(e.Message.Content, e.Message.MentionedUsers.Where(n => n.IsBot == true).Count() > 0);

        var responseMessage = await _botRunner.HandleMessage(parser);

        await e.Message.RespondAsync(responseMessage);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Connecting to Discord..");
            await _discordClient.ConnectAsync();

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