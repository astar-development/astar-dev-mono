using System.Diagnostics.CodeAnalysis;
using AStarDev.OneDriveSyncClient.Infrastructure;
using AStarDev.OneDriveSyncClient.Infrastructure.Logging;
using AStarDev.LoggingOTel;
using Avalonia;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Velopack;
using ApplicationMessages = AStarDev.LoggingExtensions.ApplicationMessages;

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

        using var loggerFactory = LoggerFactory.Create(logging => logging.ConfigureOTelLogging(configuration, ApplicationMetadata.ApplicationName));
        logger = loggerFactory.CreateLogger(ApplicationMetadata.ApplicationName);

        try
        {
            ApplicationMessages.Starting(logger, ApplicationMetadata.ApplicationName);
            var appBuilder = BuildAvaloniaApp();

            _ = appBuilder.StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            string errorType = AStarDev.LoggingExtensions.ExceptionExtensions.ErrorType(ex);
            OneDriveSyncClientMessages.ApplicationTerminatedUnexpectedly(logger, errorType);
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
                OneDriveSyncClientMessages.UnhandledApplicationException(logger, e.ExceptionObject.GetType().FullName ?? e.ExceptionObject.GetType().Name));
}
