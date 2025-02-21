﻿namespace netdaemonbot.Plugins;
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

    static readonly Regex _exIssueParsing = new(@"\s*(?'type'\w+)\s*(?'topic'.*)");

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
        }
    }

    public int Order => _order;

    public IEnumerable<(string, string?)>? GetCommandsAndDecriptions()
    {
        return
        [
            ("latest", "get latest release notes"),
            ("issues", "get latest (max 10) reported issues"),
            ("issue", "adds issues fast, enter command `issue` for additional options"),
        ];
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
                case "issues":
                    return await GetLatestIssues();
                case "todo":
                case "issue":
                    return await AddIssueInRepo(message);
            }
        }
        catch (Exception e)
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
                Text = "You have no power to add issues here, only the contributor role can do that. Please use GitHub issues at the NetDaemon repo."
            };
        }

        if (string.IsNullOrEmpty(message.CommandArgs))
        {
            return GetIssueHelpMessage();
        }

        Match? match = _exIssueParsing.Matches(message.CommandArgs).FirstOrDefault();
        if (match is not null)
        {
            string? command = null, title = null;

            foreach (Group? group in match.Groups.Cast<Group?>())
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

            return command switch
            {
                "docs" => await AddDocsIssue(title, message.User),
                "feature" => await AddDaemonIssue("feature", title, message.User),
                "bug" => await AddDaemonIssue("bug", title, message.User),
                _ => UnKnownIssueCommand(),
            };
            ;

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

        if (label is not null)
            createIssue.Labels.Add(label);

        var body = type switch
        {
            "feature" => $"{featureTemplate}\n> Added by Discord user {user}",
            "bug" => $"{issueTemplate}\n> Added by Discord user {user}",
            _ => null
        };

        if (body is not null)
            createIssue.Body = body;

        var issue = await _client.Issue.Create("net-daemon", "netdaemon", createIssue);

        if (issue is null)
        {
            return new BotResult
            {
                Title = "Failed to add issue!",
                Text = "Something technical and complicated went wrong adding issue :poop:"
            };
        }

        return new BotResult
        {
            Title = $"Success adding issue: {title}",
            Text = $"Please add details on Github here:\n<{issue.HtmlUrl}>. Undocumented issues will be closed!"
        };
    }

    private async Task<BotResult?> AddDocsIssue(string title, string user)
    {
        var createIssue = new NewIssue(title);
        createIssue.Labels.Add("documentation");
        createIssue.Body = $"{docsTemplate}\n> Added by Discord user {user}";
        
        var issue = await _client.Issue.Create("net-daemon", "docs", createIssue);

        if (issue is null)
        {
            return new BotResult
            {
                Title = "Failed to add issue!",
                Text = "Something technical and complicated went wrong adding issue :poop:"
            };
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
            Text =
                "You can manage issues if you are a contributor. A link to the created issue will be returned and you need to provide details later."
        };
        helpIssues.Fields.Add(
            ("Example", @"`issue docs Document async features of NetDaemon`, adds an issue in docs repo suggesting to document the async features. `issue feature A cool feature`, adds a new feature request in NetDaemon repo
`issue bug Failure loading`, adds a bug to the NetDaemon repo"));
        
        return helpIssues;
    }

    private async Task<BotResult?> GetVersionInfo()
    {
        var releases = await _client.Repository.Release.GetAll("net-daemon", "netdaemon");

        if (releases.Count == 0)
            return null;

        var release = releases[0];

        var result = new BotResult() { Title = $"Latest release version {release.TagName}", Text = release.Body };

        result.Fields.Add(("Author", release.Author.Login));

        return result;
    }

    private async Task<BotResult?> GetLatestIssues()
    {
        var recently = new RepositoryIssueRequest
        {
            Filter = IssueFilter.All,
            State = ItemStateFilter.Open,
        };

       // recently.Labels.Add("bug");

        var issues = await _client.Issue.GetAllForRepository("net-daemon", "netdaemon", recently);


        if (issues.Count == 0)
            return new BotResult()
            {
                Title = $"No issues!? :smiley_cat:",
                Text =
                    "No issues found! Issues can be added at <https://github.com/net-daemon/netdaemon/issues/new/choose>"
            };

        var selectedIssues = issues.Where(n => n.User.Login!="dependabot[bot]").Take(10);

        var result = new BotResult()
        {
            Title = $"Latest issues",
            Text = $"Displaying {selectedIssues.Count()} (of {selectedIssues.Count(n => n.User.Login != "dependabot[bot]")}) open issues:"
        };

        foreach (var issue in selectedIssues)
        {
            result.Fields.Add((issue.Title, $"<{issue.HtmlUrl}>"));
        }

        result.Fields.Add(("Total list of issues", "<https://github.com/net-daemon/netdaemon/issues>"));

        return result;
    }

    private readonly string featureTemplate = @"
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

    private readonly string docsTemplate = @"
<!--
    Please describe what suggestions or issues you have for the docs.
-->
## Describe your issue 


## Additional information

";
    
    private readonly string issueTemplate = @"
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
