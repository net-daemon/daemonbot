using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
///     Process static text responses to commands
/// </summary>
public class StaticCommandsPlugin : IBotPlugin
{
    private readonly int _orderOfProcessingMessages;

    public StaticCommandsPlugin(int order = 0)
    {
        _orderOfProcessingMessages = order;
    }

    public int Order => _orderOfProcessingMessages;

    public Task<BotResult?> HandleMessage(IMessage message)
    {
        if (message.Command is object && _commandResponse.ContainsKey(message.Command))
        {
            var (title, text) = _commandResponse[message.Command];

            if (title is object)
            {
                return Task.FromResult<BotResult?>(
                                           new BotResult() { Title = title, Text = text });
            }

        }
        return Task.FromResult<BotResult?>(null);
    }

    public IEnumerable<(string, string?)>? GetCommandsAndDecriptions()
    {
        return new List<(string, string?)>
        {
            ("docs", "link to the docs")
        };
    }

    private static Dictionary<string, (string title, string text)> _commandResponse = new Dictionary<string, (string, string)>
    {
        ["test"] = ("Test??", "What are you trying to test? Dont understand :zany_face: :zany_face:, use help for available commands."),
        ["die"] = (":face_with_symbols_over_mouth: Dieee?? :face_with_symbols_over_mouth:", "Why? why? whyyyyy doo you hate meeee :sob::sob::sob:, please use help for available commands."),
        ["helto"] = (":smiling_imp: helto :smiling_imp:", "Is the weirdo that actually does this for free :rofl: :rofl:, use help for available commands."),
        ["ludeeus"] = (":clap: Ludeeus :clap:", "Hangaround dev dunno what he reallys does for a living :grimacing:, use help for available commands."),
        ["netdaemon"] = (":japanese_ogre: NetDaemon :japanese_ogre:", "Yes you have come to the right server not try a better command :kissing_closed_eyes:, use help for available commands."),
        ["docs"] = (":newspaper: Docs :newspaper:", ":partying_face: <https://github.com/net-daemon/docs>")
    };
}