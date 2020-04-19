using Microsoft.Extensions.Logging;

public class HassSearchPlugin : AlgoliaPlugin
{
    public HassSearchPlugin(string? appId, string? apiKey, string? indexName, ILoggerFactory loggerFactory, string searchCommand = "search", string searchCommandHelp = "search or end with ?", string searchDescriptionHelp = "Search the docs and return top 3 results", int order = 0) : base(appId, apiKey, indexName, loggerFactory, searchCommand, searchCommandHelp, searchDescriptionHelp, order)
    {
    }
}