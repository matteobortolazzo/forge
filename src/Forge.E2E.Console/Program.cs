using Forge.E2E.Console.Models;
using Forge.E2E.Console.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Forge.E2E.Console;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        var logger = host.Services.GetRequiredService<ILogger<PipelineMonitor>>();
        var monitor = host.Services.GetRequiredService<PipelineMonitor>();

        try
        {
            var success = await monitor.RunAsync();
            return success ? 0 : 1;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Fatal error running E2E test");
            return 1;
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true);
                config.AddEnvironmentVariables();
                config.AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                // Configuration
                services.Configure<ForgeApiOptions>(context.Configuration.GetSection(ForgeApiOptions.SectionName));
                services.Configure<TestRepositoryOptions>(context.Configuration.GetSection(TestRepositoryOptions.SectionName));
                services.Configure<TimeoutOptions>(context.Configuration.GetSection(TimeoutOptions.SectionName));
                services.Configure<E2EOptions>(context.Configuration.GetSection(E2EOptions.SectionName));

                // HTTP clients
                services.AddHttpClient<ForgeApiClient>();
                services.AddHttpClient<SseEventListener>()
                    .ConfigureHttpClient(client =>
                    {
                        client.Timeout = TimeSpan.FromHours(1); // Long timeout for SSE
                    });

                // Services
                services.AddTransient<PipelineMonitor>();
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddSimpleConsole(options =>
                {
                    options.TimestampFormat = "[HH:mm:ss] ";
                    options.SingleLine = true;
                });
                logging.SetMinimumLevel(LogLevel.Information);
            });
}
