using System.Collections.Generic;
using Algolia.Search.Clients;
using System.Linq;
using System;
using System.Threading.Tasks;

public class AlgoliaSearchClient
{
    SearchClient client;
    SearchIndex index;
    public AlgoliaSearchClient()
    {
        var appId = Environment.GetEnvironmentVariable("ALGOLIA_APPID");
        var apiKey = Environment.GetEnvironmentVariable("ALGOLIA_APIKEY");
        client = new SearchClient(appId, apiKey);
        index = client.InitIndex("netdaemon");
    }

    public async Task<IEnumerable<(string, string)>> Search(string userQuery)
    {
        var query = new Algolia.Search.Models.Search.Query(userQuery);
        var result = await index.SearchAsync<Hit>(query);
        var returnList = new List<(string, string)>();
        foreach (var hit in result.Hits)
        {
            if (hit.anchor is object && hit.url is object)
                returnList.Add((hit.anchor, hit.url));
        }
        return returnList;

    }
}

public class Hit
{
    public string? anchor { get; set; }
    public object? content { get; set; }

    public Dictionary<string, object>? hierarchy { get; set; }

    public string? url { get; set; }
}
