// See https://aka.ms/new-console-template for more information

using DSharpPlus;
using netdaemonbot;
using netdaemonbot.Plugins;

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddHostedService<BotService>();
    builder.Services.AddSingleton<DiscordManager>();
    builder.Services.AddSingleton<DiscordClient>(_ => new DiscordClient(new DiscordConfiguration()
    {
        Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN"),
        TokenType = TokenType.Bot,
        Intents = DiscordIntents.GuildMessages | DiscordIntents.MessageContents | DiscordIntents.Guilds
    }));
    // Plugins
    builder.Services.AddTransient<StaticCommandsPlugin>(n =>
        new StaticCommandsPlugin(20));
    
    builder.Services.AddTransient<GithubPlugin>(n =>
        new GithubPlugin(
            n.GetRequiredService<ILoggerFactory>(),
            30,
            Environment.GetEnvironmentVariable("GITHUB_TOKEN")
        )); 
    builder.Services.AddTransient<AlgoliaPlugin>(n =>
        new AlgoliaPlugin(
            Environment.GetEnvironmentVariable("ALGOLIA_APPID"),
            Environment.GetEnvironmentVariable("ALGOLIA_APIKEY"),
            "netdaemon",
            n.GetRequiredService<ILoggerFactory>(),
             order: 10)); 
    var app = builder.Build();

    await app.RunAsync().ConfigureAwait(false);

}
catch (Exception e)
{
    Console.WriteLine($"Failed to start host... {e}");
    throw;
}