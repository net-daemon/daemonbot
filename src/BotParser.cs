﻿using System.Text.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using netdaemonbot;

namespace netdaemonbot;

public interface IBotPlugin
{
    Task<BotResult?> HandleMessage(IMessage message);
    int Order { get; }

    IEnumerable<(string, string?)>? GetCommandsAndDecriptions();
}

public interface IMessage
{
    /// <summary>
    ///     The name of the User sending message
    /// </summary>
    string User { get; }

    /// <summary>
    ///    Role of Author
    /// </summary>
    IEnumerable<string>? Roles { get; }

    /// <summary>
    ///    If author is owner
    /// </summary>
    bool IsOwner { get; }

    /// <summary>
    ///     If parsed as query to the docs this property will be non-null
    ///     (ends with ´?´)
    /// </summary>
    string? Query { get; }

    /// <summary>
    ///     True if bot is mentioned, in conversation
    /// </summary>
    bool BotMentioned { get; }

    /// <summary>
    ///     Original unparsed message
    /// </summary>
    string OriginalMessage { get; }

    /// <summary>
    ///     Parsed command
    /// </summary>
    string? Command { get; }

    /// <summary>
    ///     Parsed command arguments
    /// </summary>
    string? CommandArgs { get; }
}

/// <summary>
///     Handling parsing of incoming messages
/// </summary>
public class BotParser : IMessage
{
    /// <inheritdoc>
    public IEnumerable<string>? Roles { get; private set; }

    public bool IsOwner { get; private set; }
    public string User { get; }
    public string? Query { get; private set; }

    public bool BotMentioned { get; private set; } = false;

    public string OriginalMessage { get; private set; }

    public string? Command { get; private set; }

    public string? CommandArgs { get; private set; }

    #region -- Parse expressions --

    static Regex _exCommand = new Regex(@"(<@!\d+>)*\s*(?'command'\w+)\s*(?'argument'.*)");
    static Regex _exQuery = new Regex(@"(<@!\d+>)*\s*(?'query'.+)\?");

    #endregion

    public BotParser(string message, bool botMentioned, IEnumerable<string>? roles, bool isOwner, string user)
    {
        BotMentioned = botMentioned;
        OriginalMessage = message;
        Roles = roles;
        IsOwner = isOwner;
        User = user;

        if (message.EndsWith('?'))
        {
            // Parse queries
            Match? matchQuery = _exQuery.Matches(message).FirstOrDefault();
            if (matchQuery is object)
            {
                foreach (Group? group in matchQuery.Groups)
                {
                    if (group?.Name == "query")
                        Query = string.IsNullOrEmpty(group.Value) ? null : group.Value;
                }
            }

            return;
        }

        Match? match = _exCommand.Matches(message).FirstOrDefault();
        if (match is object)
        {
            foreach (Group? group in match.Groups)
            {
                if (group?.Name == "command")
                    Command = string.IsNullOrEmpty(group.Value) ? null : group.Value.ToLowerInvariant();
                else if (group?.Name == "argument")
                    CommandArgs = string.IsNullOrEmpty(group.Value) ? null : group.Value;
            }

        }
    }
}