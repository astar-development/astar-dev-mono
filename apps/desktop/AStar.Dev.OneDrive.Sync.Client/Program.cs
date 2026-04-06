using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Persistence;
using AStar.Dev.Utilities;
using Avalonia;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace AStar.Dev.OneDrive.Sync.Client;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might (will!) break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var pathsProvider = new LocalApplicationPathsProvider();

            string logPath = pathsProvider.LogsDirectory.CombinePath("sync.txt");

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .MinimumLevel.Information()
                .WriteTo.File(
                    formatter: new Serilog.Formatting.Json.JsonFormatter(),
                    path: logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    shared: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(1)
                )
                .CreateLogger();

            var appBuilder = BuildAvaloniaApp();

            _ = appBuilder.StartWithClassicDesktopLifetime(args);
        }
        catch(Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    // Avalonia configuration, don't remove; also used by visualdddd designer.
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
