using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace netdaemonbot;
public class DiscordManager
{
    private readonly ILogger<DiscordManager> _logger;
    private readonly DiscordClient _discordClient;
    private readonly ulong? _botChannel;
    private Func<IMessage, MessageCreateEventArgs, Task>? _onNewMessageMethod;

    public DiscordManager(ILogger<DiscordManager> logger, DiscordClient discordClient)
    {
        _logger = logger;
        _discordClient = discordClient;

        _logger.LogInformation("Initialize Discord bot...");

        var botChannel = Environment.GetEnvironmentVariable("DISCORD_BOTCHANNEL");
        
        if (ulong.TryParse(botChannel, out var channelId))
        {
            _botChannel = channelId;
            _logger.LogInformation("Subscribing to channel {ChannelId}", channelId);
            _discordClient.MessageCreated += DiscordClientOnMessageCreated;
        }
    }

    private async Task DiscordClientOnMessageCreated(DiscordClient sender, MessageCreateEventArgs e)
    {
        if (e.Message.Author.IsBot || IsThisMessageForTheBot(e) == false)
            return; // Ignore all botusers or messages not for the bot 

        DiscordMember member = (DiscordMember)e.Message.Author;
        var roles = member.Roles.Select(n => n.Name).ToList();
        var parser = new BotParser(
            e.Message.Content,
            e.Message.MentionedUsers.Where(n => n.IsBot == true).Count() > 0,
            roles,
            member.IsOwner,
            e.Author.Username
        );
       if (_onNewMessageMethod is not null) 
           await _onNewMessageMethod(parser, e); 
    }

    public async Task ConnectAsync()
    {
       
        await _discordClient.ConnectAsync();
    }
    
    public void SubscribeToMessages(Func<IMessage, MessageCreateEventArgs, Task> onNewMessageMethod)
    {
        _onNewMessageMethod = onNewMessageMethod;
    }


    /// <summary>
    ///     Returns true if the message is to the bot
    /// </summary>
    private bool IsThisMessageForTheBot(MessageCreateEventArgs e)
    {
        // Check if this is the botchannel
        if (_botChannel != null && _botChannel == e.Message.Channel.Id)
            return true;

        // Check if the bot is mentioned
        if (e.Message.MentionedUsers.Count(n => n.IsBot) > 0)
            return true;

        return false;
    }

    public async Task SendResponseMessageAsync(BotResult responseMessage, MessageCreateEventArgs e)
    {
        var embed = new DiscordEmbedBuilder
        {
            Color = new DiscordColor("#550099"),
            Title = responseMessage.Title,
            Description = (responseMessage.Text.Length < 2048) ? responseMessage.Text : responseMessage.Text[..2048]
        };
        foreach (var (field, text) in responseMessage.Fields)
        {
            embed.AddField(field, text, false);
        }
        await e.Message.RespondAsync(embed: embed.Build());
    }
}