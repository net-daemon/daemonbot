using DSharpPlus.EventArgs;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Bot
{
    static string _no_result = @":poop:  No results found!
Maybe you want to contribute that to the docs?
https://github.com/net-daemon/docs";

    public static bool IsBotChannel(MessageCreateEventArgs e)
    {
        if (e.Message.ChannelId == 699304546165588090)
        {
            return true;
        }
        return false;
    }
    public static bool IsBotUserMentioned(MessageCreateEventArgs e)
    {
        if (e.Message.MentionedRoles.Where(n => n.Id == 699326929005707355).Count() > 0 ||
            e.Message.MentionedUsers.Where(n => n.Id == 699223277683343361).Count() > 0)
        {
            return true;
        }
        return false;
    }

    public static bool IsBotUser(MessageCreateEventArgs e)
    {
        if (e.Message.Author.Id == 699223277683343361 || e.Message.Author.IsBot)
        {
            return true;
        }
        return false;
    }

    public static async Task<bool> HandleHelp(MessageCreateEventArgs e)
    {
        if (IsBotUserMentioned(e) || IsBotChannel(e))
        {
            var command = e.Message.Content;

            if (IsBotUserMentioned(e))
                command = command[23..].Trim();

            if (command.StartsWith("help", true, null))
            {
                var builder = new StringBuilder();
                // builder.AppendLine("```");
                builder.AppendLine("**Usage:** Type command to bot user or in <#699304546165588090> channel");
                builder.AppendLine("Example using bot user: @NetDaemon Bot *command*");
                builder.AppendLine("**Commands:**");
                builder.AppendLine(" - help, **displays this message**");
                builder.AppendLine(" - search or end with ?, suggests docs");
                builder.AppendLine(" - docs, link to docs");
                // builder.AppendLine("```");
                await e.Message.RespondAsync(builder.ToString());
                return true;
            }
        }
        return false;
    }
    private static Dictionary<string, string> _commandResponse = new Dictionary<string, string>
    {
        ["test"] = "What are you trying to test? Dont understand :zany_face: :zany_face:, use help for available commands.",
        ["helto"] = "Is the weirdo that actually does this for free :rofl: :rofl:, use help for available commands.",
        ["ludeeus"] = "Hangaround dev dunno what he reallys does for a living :grimacing:, use help for available commands.",
        ["netdaemon"] = "Yes you have come to the right server not try a better command :kissing_closed_eyes:, use help for available commands.",
        ["docs"] = ":partying_face: https://github.com/net-daemon/docs"
    };
    public static async Task<bool> HandleCommandsPeopleMightWrite(MessageCreateEventArgs e)
    {
        if (IsBotUserMentioned(e) || IsBotChannel(e))
        {
            var command = e.Message.Content;

            if (IsBotUserMentioned(e))
                command = command[23..].Trim().ToLowerInvariant();

            if (_commandResponse.ContainsKey(command))
            {
                await e.Message.RespondAsync(_commandResponse[command]);
                return true;
            }
        }
        return false;
    }
    public static async Task<bool> HandleSupportQueries(MessageCreateEventArgs e, SearchBot sbot)
    {
        if (IsBotUserMentioned(e) || IsBotChannel(e))
        {
            var query = e.Message.Content;

            if (IsBotUserMentioned(e))
                query = query = e.Message.Content[23..].Trim();

            var useSearch = query.StartsWith("search", true, null);

            if (query.EndsWith('?') || useSearch)
            {

                if (useSearch)
                    query = e.Message.Content[7..];

                await SendSupportResponse(e, sbot, query.Trim());
                return true;
            }

        }

        return false;

        async Task SendSupportResponse(MessageCreateEventArgs e, SearchBot sbot, string query)
        {
            try
            {
                //It is a query
                var searchResult = await sbot.Search(query);
                var builder = new StringBuilder();

                if (searchResult.Count() == 0)
                {
                    await e.Message.RespondAsync(_no_result);
                    return;
                }

                builder.AppendLine($"I found {searchResult.Take(3).Count()} results for you :partying_face:");
                foreach (var (name, url) in searchResult.Take(3))
                {
                    builder.AppendLine($"**{name}**");
                    builder.AppendLine($"{url}");

                }
                await e.Message.RespondAsync(builder.ToString());
            }
            catch (System.Exception)
            {
                // Ignore errors for now. Todo: fix!
            }



        }
    }
}