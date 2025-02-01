namespace netdaemonbot.Plugins;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
///     Process static text responses to commands
/// </summary>
public class StaticCommandsPlugin(int order = 0) : IBotPlugin
{
    private readonly int _orderOfProcessingMessages = order;

    public int Order => _orderOfProcessingMessages;

    public Task<BotResult?> HandleMessage(IMessage message)
    {
        if (message.Command is object && _commandResponse.TryGetValue(message.Command, out (string title, string text) value))
        {
            var (title, text) = value;

            if (title is not null)
            {
                return Task.FromResult<BotResult?>(
                                           new BotResult() { Title = title, Text = text });
            }

        }
        return Task.FromResult<BotResult?>(null);
    }

    public IEnumerable<(string, string?)>? GetCommandsAndDecriptions()
    {
        return
        [
            ("docs", "show me the url to the docs")
        ];
    }

    private static Dictionary<string, (string title, string text)> _commandResponse = new Dictionary<string, (string, string)>
    {
        ["test"] = ("Test??", "What are you trying to test? Dont understand? Are you one of those overachievers that tests everything? :zany_face: :zany_face:, use help for available commands."),
        ["die"] = (":face_with_symbols_over_mouth: Dieee?? :face_with_symbols_over_mouth:", "Why? why? whyyyyy doo you hate meeee :sob::sob::sob:, please use help for available commands."),
        ["helto"] = (":smiling_imp: helto :smiling_imp:", "Is the weirdo that actually does this for free :rofl: :rofl:, use help for available commands."),
        ["ludeeus"] = (":clap: Ludeeus :clap:", "Hangaround dev dunno what he really does for a living :grimacing:, use help for available commands."),
        ["frank"] = (":clap: Frank Bakker :clap:", "The genius behind the HassModel, treat him with respect, use help for available commands."),
        ["netdaemon"] = (":japanese_ogre: NetDaemon :japanese_ogre:", "Yes you have come to the right server not try a better command :kissing_closed_eyes:, use help for available commands."),
        ["docs"] = (":newspaper: Docs :newspaper:", ":partying_face: <https://netdaemon.xyz>")
    };
} 
