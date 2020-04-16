using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using Octokit;

/// <summary>
///     Process static text responses to commands
/// </summary>
public class GithubPlugin : IBotPlugin
{
    private readonly ILogger<GithubPlugin> _logger;
    private readonly GitHubClient _client;
    private readonly int _order;

    // private readonly bool _isAuthenticated;
    public GithubPlugin(ILoggerFactory loggerFactory, int order, string? githubToken = null)
    {
        _logger = loggerFactory.CreateLogger<GithubPlugin>();

        _client = new GitHubClient(new ProductHeaderValue("NetDaemonBot"));

        _order = order;

        var token = githubToken;

        if (string.IsNullOrEmpty(token) == false)
        {
            _client.Credentials = new Credentials(token);
            // _isAuthenticated = true;
        }
    }

    public int Order => _order;

    public IEnumerable<(string, string?)>? GetCommandsAndDecriptions()
    {
        return new List<(string, string?)>
        {
            ("latest", "get latest version information"),
            ("bugs", "get latest (max 5) reported open bugs")
        };
    }

    public async Task<BotResult?> HandleMessage(IMessage message)
    {

        try
        {
            if (message.Command is null)
                return null;

            switch (message.Command)
            {
                case "latest":
                    return await GetVersionInfo();
                case "bugs":
                    return await GetLatestBugs();
            }
        }
        catch (System.Exception e)
        {
            _logger.LogError(e, "Failed to handle message");
        }
        return null;
    }

    private async Task<BotResult?> GetVersionInfo()
    {
        var releases = await _client.Repository.Release.GetAll("net-daemon", "netdaemon");

        if (releases.Count == 0)
            return null;

        var release = releases.First();

        var result = new BotResult() { Title = $"Latest release version {release.TagName}", Text = release.Body };

        result.Fields.Add(("Author", release.Author.Login));

        return result;
    }

    private async Task<BotResult?> GetLatestBugs()
    {
        var recently = new RepositoryIssueRequest
        {
            Filter = IssueFilter.All,
            State = ItemStateFilter.Open,
        };

        recently.Labels.Add("bug");

        var bugs = await _client.Issue.GetAllForRepository("net-daemon", "netdaemon", recently);


        if (bugs.Count == 0)
            return new BotResult() { Title = $"Yay! :smiley_cat:", Text = "No open filed bugs! Please report if you find an issue at <https://github.com/net-daemon/netdaemon/issues/new/choose>" };

        var result = new BotResult() { Title = $"What?? There is bugs?? :bug:", Text = "Following last 5 open issues labeled bug found!" };

        foreach (var bug in bugs.Take(5))
        {
            result.Fields.Add((bug.Title, $"<{bug.HtmlUrl}>"));
        }

        // result.Fields.Add(("Author", release.Author.Login));

        return result;
    }
}