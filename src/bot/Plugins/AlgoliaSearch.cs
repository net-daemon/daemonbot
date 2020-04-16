
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Algolia.Search.Clients;
using Microsoft.Extensions.Logging;

/// <summary>
///     Implements search capabilities using Algolia search API
/// </summary>
public class AlgoliaPlugin : IBotPlugin
{
    static BotResult _defaultEmptyResultMessage = new BotResult
    {
        Title = ":poop: No results found!",
        Text = "Maybe you want to contribute that to the docs?\n<https://github.com/net-daemon/docs>"
    };

    private int _orderOfProcessingMessages = 0;
    private ILogger _logger;
    SearchClient _algoliaSearchClient;
    SearchIndex _algoliaIndex;

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="appId">Application id provided by Algolia</param>
    /// <param name="apiKey">API Key provided by Algolia</param>
    /// <param name="indexName">Name of index used for queries</param>
    /// <param name="loggerFactory">Logger factory</param>
    /// <param name="order">The order this plugin should process incoming messages</param>
    public AlgoliaPlugin(
        string? appId,
        string? apiKey,
        string? indexName,
        ILoggerFactory loggerFactory,
        int order = 0)
    {
        _ = appId ?? throw new ArgumentNullException(nameof(appId));
        _ = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _ = indexName ?? throw new ArgumentNullException(nameof(apiKey));

        _logger = loggerFactory.CreateLogger<AlgoliaPlugin>();
        _algoliaSearchClient = new SearchClient(appId, apiKey);
        _algoliaIndex = _algoliaSearchClient.InitIndex(indexName);
        _orderOfProcessingMessages = order;
    }

    public int Order => _orderOfProcessingMessages;

    public async Task<BotResult?> HandleMessage(IMessage message)
    {
        if (message.Command == "search")
        {
            if (message.CommandArgs is object)
                return await QueryMessage(message.CommandArgs);
            else
                return new BotResult
                {
                    Title = ":poop: Meeeh!",
                    Text = "Hey you! You want me to guess what you wanted to search for?? Please provide me more information!"
                };
        }
        else if (message.Query is object)
        {
            return await QueryMessage(message.Query);
        }
        return null;
    }

    public IEnumerable<(string, string?)>? GetCommandsAndDecriptions()
    {
        return new List<(string, string?)>
        {
            ("search or any word/s end with ?", "suggests docs from NetDaemon docs")
        };
    }

    private async Task<BotResult> QueryMessage(string query)
    {
        try
        {

            var searchResult = await Search(query);

            var builder = new StringBuilder();

            if (searchResult.Count() == 0)
            {
                return _defaultEmptyResultMessage;
            }

            var botResult = new BotResult
            {
                Title = $"I found {searchResult.Take(3).Count()} results for you :partying_face:",
            };

            builder.AppendLine();
            foreach (var (topic, url) in searchResult.Take(3))
            {
                botResult.Fields.Add((topic, url));
                // builder.AppendLine($"**{topic}**");
                // builder.AppendLine($"<{url}>");
            }
            return botResult;
        }
        catch (System.Exception)
        {
            // Ignore errors for now. Todo: fix!
        }
        return _defaultEmptyResultMessage;
    }

    private async Task<IEnumerable<(string, string)>> Search(string userQuery)
    {
        var returnList = new List<(string, string)>();
        try
        {
            var query = new Algolia.Search.Models.Search.Query(userQuery);
            // query.
            var result = await _algoliaIndex.SearchAsync<Hit>(query);

            foreach (var hit in result.Hits)
            {
                if (hit.hierarchy is object && hit.url is object)
                {
                    bool isIndexedWrong = false;

                    var caption = hit.anchor;
                    for (int i = 5; i >= 0; i--)
                    {
                        var lvl = $"lvl{i}";
                        if (hit.hierarchy.ContainsKey(lvl) && hit.hierarchy[lvl] is object)
                        {
                            caption = hit.hierarchy[lvl];

                            if (caption.StartsWith("«") || caption.EndsWith("»"))
                                isIndexedWrong = true; // It has indexed the arrow texts

                            break;
                        }
                    }

                    if (hit.url.EndsWith("#__docusaurus"))
                    {
                        hit.url = hit.url[..^14];
                    }

                    // Compensate for fauly indexed pages
                    if (!isIndexedWrong)
                        returnList.Add((caption!, hit.url));
                }
            }
        }
        catch (System.Exception e)
        {
            _logger.LogError(e, "Ops, something went wrong in Algolia search!");
        }
        return returnList;
    }
}

/// <summary>
///     Used to parse results from Algolia search API
/// </summary>
public class Hit
{
    public string? anchor { get; set; }
    public string? url { get; set; }
    public Dictionary<string, string>? hierarchy { get; set; }
}