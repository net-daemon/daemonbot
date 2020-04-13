using DSharpPlus.EventArgs;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Bot
{
    static string _no_result = @":poop:  No results found!
Maybe you want to contribute that to the docs?
https://github.com/net-daemon/docs";

    public static async Task<bool> HandleSupportQueries(MessageCreateEventArgs e, SearchBot sbot)
    {
        if (e.Message.MentionedRoles.Select(n => n.Id == 699226092426231858).Count() > 0 ||
            e.Message.MentionedUsers.Select(n => n.Id == 699223277683343361).Count() > 0)
        {
            var useSearch = e.Message.Content.StartsWith("<@&699226092426231858> search", true, null) ||
                            e.Message.Content.StartsWith("<@!699223277683343361> search", true, null);

            if (e.Message.Content.EndsWith('?') || useSearch)
            {
                string query = "";

                if (useSearch)
                    query = e.Message.Content[30..];
                else
                    query = e.Message.Content[23..];

                await SendSupportResponse(e, sbot, query.Trim());
                return true;
            }

        }
        if (e.Message.ChannelId == 1234456777)
        {
            await SendSupportResponse(e, sbot, e.Message.Content[23..]);
            return true;

        }

        return false;

        async Task SendSupportResponse(MessageCreateEventArgs e, SearchBot sbot, string query)
        {
            try
            {
                //It is a query
                var searchResult = await sbot.Search(query);

                if (searchResult.Count() == 0)
                {
                    await e.Message.RespondAsync(_no_result);
                }
                var builder = new StringBuilder();
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