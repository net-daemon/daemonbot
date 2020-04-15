using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

public interface IBotRunner
{
    Task<string> HandleMessage(IMessage message);
}
public class BotRunner : IBotRunner
{

    private readonly ILogger _logger;
    private readonly IEnumerable<IBotPlugin> _plugins;
    private readonly IServiceProvider _provider;

    public BotRunner(IServiceProvider provider)
    {
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger<BotRunner>();
        _provider = provider;
        _plugins = GetPluginsFromExecutingAssembly();
    }

    public IEnumerable<IBotPlugin> GetPluginsFromExecutingAssembly()
    {
        var pluginTypes = from t in Assembly.GetExecutingAssembly().GetTypes()
                          where t.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IBotPlugin))
                          select t;
        var pluginList = new List<IBotPlugin>();
        foreach (Type pluginType in pluginTypes)
        {
            var pluginInstance = (IBotPlugin)_provider.GetService(pluginType);
            pluginList.Add(pluginInstance);
        }
        return pluginList.OrderBy(n => n.Order);
    }

    public async Task<string> HandleMessage(IMessage message)
    {
        if (message.Command == "help" && message.CommandArgs is null)
            return HelpMessage();

        foreach (var plugin in _plugins)
        {
            var returnMessage = await plugin.HandleMessage(message);
            if (returnMessage is object)
            {
                return returnMessage;
            }
        }
        return DefaultMessage();
    }

    public string HelpMessage()
    {
        var builder = new StringBuilder();
        builder.AppendLine("**Usage:** Type command to bot user or in the bot channel");
        builder.AppendLine("**Commands:**");
        builder.AppendLine(" - help, displays this message :smile:");

        foreach (var plugin in _plugins)
        {
            var pluginCommands = plugin.GetCommandsAndDecriptions();
            if (pluginCommands is object)
                foreach (var (command, description) in pluginCommands)
                    builder.AppendLine($" - {command}, {description}");
        }

        return builder.ToString();
    }
    public static string DefaultMessage()
    {
        return ":poop:I am sorry I could not understand your command, type command **help** for valid commands";
    }
}