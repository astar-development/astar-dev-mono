using System.Diagnostics.CodeAnalysis;
using AStarDev.OneDriveSyncClient.Infrastructure;
using AStarDev.LoggingOTel;
using Avalonia;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Velopack;
using ApplicationMessages = AStar.Dev.Logging.Extensions.ApplicationMessages;
using LogMessage = AStar.Dev.Logging.Extensions.LogMessage;

namespace AStarDev.OneDriveSyncClient;

[ExcludeFromCodeCoverage]
internal static class Program
{
    private static ILogger logger = NullLogger.Instance;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might (will!) break.
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        using var loggerFactory = LoggerFactory.Create(logging => logging.ConfigureOTelLogging(configuration));
        logger = loggerFactory.CreateLogger(ApplicationMetadata.ApplicationName);

        try
        {
            ApplicationMessages.Starting(logger, ApplicationMetadata.ApplicationName);
            var appBuilder = BuildAvaloniaApp();

            _ = appBuilder.StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            LogMessage.Error(logger, "Application terminated unexpectedly", ex);
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .With(new X11PlatformOptions { EnableIme = false })
            .AfterSetup(_ => AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                LogMessage.Error(logger, $"[Unhandled] {(e.ExceptionObject as Exception)?.Message ?? "Unknown"}", e.ExceptionObject as Exception ?? new InvalidOperationException("Unknown unhandled exception")));
}
