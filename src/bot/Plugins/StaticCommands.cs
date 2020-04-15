using System.Collections.Generic;
using System.Text;
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

    public Task<string?> HandleMessage(IMessage message)
    {
        if (message.Command is object)
            return Task.FromResult<string?>(
                _commandResponse.ContainsKey(message.Command) ? _commandResponse[message.Command] : null);

        return Task.FromResult<string?>(null);
    }

    public IEnumerable<(string, string?)>? GetCommandsAndDecriptions()
    {
        return new List<(string, string?)>
        {
            ("docs", "link to the docs")
        };
    }

    private static Dictionary<string, string> _commandResponse = new Dictionary<string, string>
    {
        ["test"] = "What are you trying to test? Dont understand :zany_face: :zany_face:, use help for available commands.",
        ["self destruct"] = "Ok selfdestructing in 5..4..3..2..1.. naaah, please use help for available commands.",
        ["meaning of life"] = "42 :partying_face:, please use help for available commands.",
        ["die"] = "Why? why? whyyyyy doo you hate meeee :sob::sob::sob:, please use help for available commands.",
        ["helto"] = "Is the weirdo that actually does this for free :rofl: :rofl:, use help for available commands.",
        ["ludeeus"] = "Hangaround dev dunno what he reallys does for a living :grimacing:, use help for available commands.",
        ["netdaemon"] = "Yes you have come to the right server not try a better command :kissing_closed_eyes:, use help for available commands.",
        ["docs"] = ":partying_face: https://github.com/net-daemon/docs"
    };
}