using System.Text;
using Algolia.Search.Clients;
using Algolia.Search.Models.Search;
using netdaemonbot;

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
    /*SearchIndex _algoliaIndex;*/
    private readonly string _searchCommand;
    private readonly bool _isDefaultSearch;
    private readonly string _searchCommandHelp;
    private readonly string _searchDescriptionHelp;
    private readonly string _indexName;

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
        string searchCommand = "search",
        string searchCommandHelp = "search",
        string searchDescriptionHelp = "searches the docs and return top 3 results, you can also search by just typing your query followed by the `?` character",
        int order = 0)
    {
        _ = appId ?? throw new ArgumentNullException(nameof(appId));
        _ = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _indexName = indexName ?? throw new ArgumentNullException(nameof(apiKey));

        _searchCommand = searchCommand;
        _isDefaultSearch = searchCommand == "search" ? true : false;

        _searchCommandHelp = searchCommandHelp;
        _searchDescriptionHelp = searchDescriptionHelp;

        _logger = loggerFactory.CreateLogger<AlgoliaPlugin>();
        _algoliaSearchClient = new SearchClient(appId, apiKey);
        /*_algoliaIndex = _algoliaSearchClient.InitIndex(indexName);*/

        _orderOfProcessingMessages = order;
    }

    public int Order => _orderOfProcessingMessages;

    public async Task<BotResult?> HandleMessage(IMessage message)
    {
        if (message.Command == _searchCommand)
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
        else if (message.Query is object && _isDefaultSearch)
        {
            return await QueryMessage(message.Query);
        }
        return null;
    }

    public IEnumerable<(string, string?)>? GetCommandsAndDecriptions()
    {
        return new List<(string, string?)>
        {
            (_searchCommandHelp, _searchDescriptionHelp)
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
            var query = new SearchQuery(
                    new SearchForHits
                    {
                        IndexName = _indexName,
                        Query = userQuery,
                        HitsPerPage = 10,
                    });
            var result = await _algoliaSearchClient.SearchAsync<Hit>(
                    new SearchMethodParams
                    {
                        Requests = [query]
                    });

            foreach (var res in result.Results)
            {
                foreach (var hit in res.AsSearchResponse().Hits)
                {
                    if (hit.hierarchy is not null && hit.url is not null)
                    {
                        bool isIndexedWrong = false;

                        var caption = hit.anchor;
                        for (int i = 5; i >= 0; i--)
                        {
                            var lvl = $"lvl{i}";
                            if (hit.hierarchy.TryGetValue(lvl, out string? value) && value is not null)
                            {
                                caption = value;

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
        }
        catch (Exception e)
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
