namespace netdaemonbot;

public class BotResult
{
    /// <summary>
    ///     The message title
    /// </summary>
    /// <value></value>
    public string Title { get; set; } = "";

    /// <summary>
    ///     Text to display in message
    /// </summary>
    public string Text { get; set; } = "";

    /// <summary>
    ///     Fields
    /// </summary>
    /// <typeparam name="field">Field name</typeparam>
    /// <typeparam name="text">Text in field</typeparam>
    /// <returns></returns>
    public List<(string field, string text)> Fields { get; set; } = [];

}
