using System.Diagnostics.CodeAnalysis;
using AStarDev.OneDriveSyncClient.Infrastructure;
using AStarDev.OneDriveSyncClient.Infrastructure.Startup;
using AStarDev.LoggingSerilog;
using Avalonia;
using Microsoft.Extensions.Configuration;
using Serilog;
using Velopack;

namespace AStarDev.OneDriveSyncClient;

[ExcludeFromCodeCoverage]
internal static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might (will!) break.
    [STAThread]
    public static async Task Main(string[] args)
    {
        VelopackApp.Build().Run();

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            _ = Directory.CreateDirectory(ApplicationDirectories.LogsDirectory);
            Log.Logger = SerilogConfigurator.CreateLogger(configuration, $"{ApplicationDirectories.LogsDirectory}/{ApplicationMetadata.ApplicationLogName}", RollingInterval.Hour, 7);

            Log.Information("Application Starting");
            var appBuilder = BuildAvaloniaApp();

            _ = appBuilder.StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .With(new X11PlatformOptions { EnableIme = false })
            .AfterSetup(_ => AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    Log.Fatal(e.ExceptionObject as Exception, "[Unhandled] {Message}", (e.ExceptionObject as Exception)?.Message ?? "Unknown");
                    Log.CloseAndFlush();
                });
}
