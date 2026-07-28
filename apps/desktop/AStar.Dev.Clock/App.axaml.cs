using AStar.Dev.Velopack.Publishing.Avalonia.Updates;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.Clock;

public partial class App : Application, IDisposable
{
    private ServiceProvider? services;
    private bool disposed;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        services = BuildServices();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();

            _ = services.GetRequiredService<IUpdateNotificationService>().CheckAndNotifyAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }

    public void SetTheme(ThemeVariant? variant) => RequestedThemeVariant = variant ?? ThemeVariant.Default; // Default == Auto

    private static ServiceProvider BuildServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        return new ServiceCollection()
            .AddApplicationServices(configuration)
            .BuildServiceProvider();
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        services?.Dispose();

        GC.SuppressFinalize(this);
    }
}
