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
                .ConfigureServices(services => { services.AddHostedService<BotService>(); });
    }
}
