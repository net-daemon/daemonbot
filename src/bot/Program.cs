using Algolia.Search.Clients;
using Algolia.Search.Http;
using DSharpPlus;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Serilog.Core;

namespace bot
{

    public class Program
    {
        // Logging switch
        private static LoggingLevelSwitch _levelSwitch = new LoggingLevelSwitch();

        // public static async Task Main(string[] args)
        public static void Main(string[] args)
        {

            try
            {
                // Setup serilog
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .MinimumLevel.ControlledBy(_levelSwitch)
                    .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                    .CreateLogger();

                var envLogLevel = Environment.GetEnvironmentVariable("BOT_LOG_LEVEL");
                _levelSwitch.MinimumLevel = envLogLevel switch
                {
                    "info" => LogEventLevel.Information,
                    "debug" => LogEventLevel.Debug,
                    "error" => LogEventLevel.Error,
                    "warning" => LogEventLevel.Warning,
                    "trace" => LogEventLevel.Verbose,
                    _ => LogEventLevel.Information
                };

                Log.Information("Starting BotServiceHost...");

                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception e)
            {
                Log.Fatal(e, "Failed to start BotServiceHost...");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices(services =>
                {
                    services.AddHostedService<BotService>();


                    services.AddTransient<AlgoliaPlugin>(n =>
                        new AlgoliaPlugin(
                            Environment.GetEnvironmentVariable("ALGOLIA_APPID"),
                            Environment.GetEnvironmentVariable("ALGOLIA_APIKEY"),
                            "netdaemon",
                            n.GetRequiredService<ILoggerFactory>(),
                             order: 10));

                    services.AddTransient<HassSearchPlugin>(n =>
                        new HassSearchPlugin(
                            Environment.GetEnvironmentVariable("ALGOLIA_APPID"),
                            "ae96d94b201c5444c8a443093edf3efb",
                            "home-assistant",
                            n.GetRequiredService<ILoggerFactory>(),
                            searchCommand: "hass",
                            searchCommandHelp: "hass",
                            searchDescriptionHelp: "Type hass and search word/s to get search results from Home Assistant docs.",
                            order: 10));

                    services.AddTransient<StaticCommandsPlugin>(n =>
                        new StaticCommandsPlugin(20));

                    services.AddTransient<GithubPlugin>(n =>
                                            new GithubPlugin(
                                                n.GetRequiredService<ILoggerFactory>(),
                                                30,
                                                Environment.GetEnvironmentVariable("GITHUB_TOKEN")
                                                ));

                    services.AddSingleton<IBotRunner, BotRunner>();

                    // Todo: Find out home assistant app_id to add support for Hass search later

                });
    }
}
