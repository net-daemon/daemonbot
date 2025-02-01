using System.Reflection;
using System.Text;
using DSharpPlus;

namespace netdaemonbot;

public class BotService(ILogger<BotService> logger, DiscordManager manager, IServiceProvider provider) : IHostedService
{
    private IEnumerable<IBotPlugin>? _plugins;

    public IEnumerable<IBotPlugin> GetPluginsFromExecutingAssembly()
    {
        var pluginTypes = from t in Assembly.GetExecutingAssembly().GetTypes()
            where t.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IBotPlugin))
            select t;
        var pluginList = new List<IBotPlugin>();

        foreach (Type pluginType in pluginTypes)
        {
            var pluginInstance = (IBotPlugin)provider.GetRequiredService(pluginType);
            pluginList.Add(pluginInstance);
        }

        return pluginList.OrderBy(n => n.Order);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting BotService...");

        _plugins = GetPluginsFromExecutingAssembly();

        try
        {
            logger.LogInformation("Connecting to Discord..");
            await manager.ConnectAsync();

            manager.SubscribeToMessages(async (message, e) =>
            {
                if (message is { Command: "help", CommandArgs: null })
                {
                    await manager.SendResponseMessageAsync(HelpMessage(), e); 
                    return;
                }
                    
                foreach (var plugin in _plugins)
                {
                    var returnMessage = await plugin.HandleMessage(message);
                    if (returnMessage is not null)
                    {
                        await manager.SendResponseMessageAsync(returnMessage, e);
                        return;
                    }
                }
                await manager.SendResponseMessageAsync(DefaultMessage(), e);
            });
            logger.LogInformation("Bot is running and receiving messages!");
        }
        catch (OperationCanceledException)
        {
            // Ignore, normal shutdown
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unhandled error in BotService, exiting..");
            return;
        }
    }

    public BotResult HelpMessage()
    {
        var result = new BotResult()
        {
            Title = "Help",
        };
        result.Fields.Add(("Usage", "Type command to bot user or in the bot channel"));

        var builder = new StringBuilder();

        // builder.AppendLine("**Commands:**");
        builder.AppendLine(">>> - **help**, displays this message :smile:");

        foreach (var plugin in _plugins)
        {
            var pluginCommands = plugin.GetCommandsAndDecriptions();
            if (pluginCommands is not null)
                foreach (var (command, description) in pluginCommands)
                    builder.AppendLine($"- **{command}**, {description}");
        }

        result.Fields.Add(("Commands", builder.ToString()));

        return result;
    }
    public BotResult DefaultMessage()
    {
        var result = new BotResult()
        {
            Title = ":poop: Whut??",
        };
        result.Fields.Add(("Unknown command", "Unknown command! If you are trying to search in the docs, type `search` followed by your search query or just type query and end with a `?` character."));
        var builder = new StringBuilder();

        foreach (var plugin in _plugins)
        {
            var pluginCommands = plugin.GetCommandsAndDecriptions();
            if (pluginCommands is not null)
                foreach (var (command, description) in pluginCommands)
                    builder.AppendLine($"- **{command}**, {description}");
        }

        result.Fields.Add(("Available commands", builder.ToString()));

        return result;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping BotService...");
        return Task.CompletedTask;
    }
}
