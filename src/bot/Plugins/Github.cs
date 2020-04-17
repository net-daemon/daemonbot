using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using Octokit;
using System.Text.RegularExpressions;

/// <summary>
///     Process static text responses to commands
/// </summary>
public class GithubPlugin : IBotPlugin
{
    private readonly ILogger<GithubPlugin> _logger;
    private readonly GitHubClient _client;
    private readonly int _order;

    static Regex _exIssueParsing = new Regex(@"\s*(?'type'\w+)\s*(?'topic'.*)");
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
            ("bugs", "get latest (max 5) reported open bugs"),
            ("issue", "Manage issues in repos. Enter command issue for help")
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
                case "todo":
                case "issue":
                    return await AddIssueInRepo(message);
            }
        }
        catch (System.Exception e)
        {
            _logger.LogError(e, "Failed to handle message");
        }
        return null;
    }

    private async Task<BotResult?> AddIssueInRepo(IMessage message)
    {
        if (message.Roles.Contains("contributor") == false && message.IsOwner == false)
        {
            return new BotResult
            {
                Title = ":poop: Sorry mate!",
                Text = "You have no power to add issues here...Please use GitHub Issues"
            };
        }
        if (string.IsNullOrEmpty(message.CommandArgs))
        {
            return GetIssueHelpMessage();
        }

        Match? match = _exIssueParsing.Matches(message.CommandArgs).FirstOrDefault();
        if (match is object)
        {
            string? command = null, title = null;

            foreach (Group? group in match.Groups)
            {
                if (group?.Name == "type")
                    command = string.IsNullOrEmpty(group.Value) ? null : group.Value.ToLowerInvariant();
                else if (group?.Name == "topic")
                    title = string.IsNullOrEmpty(group.Value) ? null : group.Value;
            }

            if (title is null)
            {
                return GetIssueMissingTitleHelpMessage();
            }

            switch (command)
            {
                case "docs":
                    return await AddDocsIssue(title);
                case "feature":
                    return await AddDaemonIssue("feature", title, message.User);
                case "bug":
                    return await AddDaemonIssue("bug", title, message.User);
                default:
                    return UnKnownIssueCommand();
            };

        }
        return new BotResult
        {
            Title = ":poop: failed to parse issue sub-command",
            Text = "Adding a issue require a title. `issue [sub-command] [Title]` is the correct format."
        };
    }

    private async Task<BotResult?> AddDaemonIssue(string type, string title, string user)
    {
        var createIssue = new NewIssue(title);

        createIssue.Labels.Add("bot");

        var label = type switch
        {
            "feature" => "feature request",
            "bug" => "bug",
            _ => null
        };

        if (label is object)
            createIssue.Labels.Add(label);

        var body = type switch
        {
            "feature" => $"{featureTemplate}\n> Added by Discord user {user}",
            "bug" => $"{issueTemplate}\n> Added by Discord user {user}",
            _ => null
        };

        if (body is object)
            createIssue.Body = body;

        var issue = await _client.Issue.Create("net-daemon", "netdaemon", createIssue);

        if (issue is null)
        {
            return new BotResult { Title = "Failed to add issue!", Text = "Something technical and complicated went wrong adding issue :poop:" };
        }

        return new BotResult
        {
            Title = $"Success adding issue: {title}",
            Text = $"Please add details on Github here:\n<{issue.HtmlUrl}>. Undocumented issues will be closed!"
        };
    }

    private async Task<BotResult?> AddDocsIssue(string title)
    {
        var createIssue = new NewIssue(title);
        createIssue.Labels.Add("documentation");
        var issue = await _client.Issue.Create("net-daemon", "docs", createIssue);

        if (issue is null)
        {
            return new BotResult { Title = "Failed to add issue!", Text = "Something technical and complicated went wrong adding issue :poop:" };
        }

        return new BotResult
        {
            Title = $"Success adding issue: {title}",
            Text = $"Please add details on Github here:\n<{issue.HtmlUrl}>. Undocumented issues will be closed!"
        };
    }


    private BotResult GetIssueMissingTitleHelpMessage()
    {
        return new BotResult
        {
            Title = ":poop: Issue, missing title",
            Text = "Adding a issue require a title. issue `[command] [Title]` is the correct format."
        };
    }

    private BotResult UnKnownIssueCommand()
    {
        var result = new BotResult
        {
            Title = ":poop: Issue, unknown subcommand",
            Text = "Adding a issue require a title. `issue [sub-command] [Title]` is the correct format."
        };
        result.Fields.Add(("Available sub-commands", "docs, feature, bug"));
        return result;
    }


    private BotResult GetIssueHelpMessage()
    {
        var helpIssues = new BotResult
        {
            Title = "Help - Issue",
            Text = "You can manage issues if you are a contributor. A link to the created issue will be returned and you need to provide details later."
        };
        helpIssues.Fields.Add(
            ("Example", @"`issue docs document storage better`, adds an issue in docs repo to document storage better
`issue feature A cool feature`, adds a new feature request in NetDaemon repo
`issue bug Failure loading`, adds a bug to the NetDaemon repo"));

        return helpIssues;
    }

    private async Task<BotResult?> GetVersionInfo()
    {
        var releases = await _client.Repository.Release.GetAll("net-daemon", "netdaemon");

        if (releases.Count == 0)
            return null;

        var release = releases.First();

        var result = new BotResult() { Title = $"Latest release version {release.TagName}", Text = release.Body };

        result.Fields.Add(("Install latest dev components",
            $"dotnet add package JoySoftware.NetDaemon.App --version {release.TagName}-alpha\ndotnet add package JoySoftware.NetDaemon.DaemonRunner --version {release.TagName}-alpha"));
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

        var selectedBugs = bugs.Take(5);

        var result = new BotResult() { Title = $"What?? There is bugs?? :bug:", Text = $"Following last {selectedBugs.Count()} open issues labeled bug found!" };

        foreach (var bug in selectedBugs)
        {
            result.Fields.Add((bug.Title, $"<{bug.HtmlUrl}>"));
        }

        // result.Fields.Add(("Author", release.Author.Login));

        return result;
    }
    private string featureTemplate = @"
<!--
    Please describe the feature you want from a usage perspective.
-->
## Describe your feature


<!--
    Please use example code if applicable.
-->
## Example code
```c#
// Insert any example code here that will help describe your requests

```

## Additional information

";

    private string issueTemplate = @"
<!-- READ THIS FIRST:
  - If you need additional help with this template, please refer to https://netdaemon.xtz/help/reporting_issues/
  - Make sure you are running the latest version of NetDaemon before reporting an issue: https://github.com/net-daemon/netdaemon/releases
  - Do not use issues for support, we have the discord server for that purpose. https://discord.gg/K3xwfcX
  - Provide as many details as possible. Paste logs, configuration samples and code into the backticks.
  DO NOT DELETE ANY TEXT from this template! Otherwise, your issue may be closed without comment.
-->
## The problem
<!--
  Describe the issue you are experiencing here to communicate to the
  maintainers. Tell us what you were trying to do and what happened.
-->


## Environment
<!--
  Provide details about the versions you are using, which helps us to reproduce
  and find the issue quicker. Version information is found in the
  in the logs at startup.
-->

- NetDaemon release with the issue:
- Last working NetDaemon release (if known):
- Operating environment (Home assistant Add-on/Docker/Dev setup):
- Link to integration documentation on our website:

## Link to or paste code that causes the issue
<!--
  Example of code that causes the issue.
-->

```c#

```

## Traceback/Error logs
<!--
  If you come across any trace or error logs, please provide them.
-->

```txt

```

## Additional information
";
}