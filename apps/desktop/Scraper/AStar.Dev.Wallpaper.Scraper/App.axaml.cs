using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scraper.Models;
using AStar.Dev.Wallpaper.Scraper.Services;
using AStar.Dev.Wallpaper.Scraper.Support;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AStar.Dev.Wallpaper.Scraper;

public partial class App : Application
{
    private IHost host = null!;

    public static new App Current => (App)Application.Current!;
    public IServiceProvider Services => host.Services;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override async void OnFrameworkInitializationCompleted()
    {
        host = AppCompositionRoot.CreateHost();

        await host.Services.GetRequiredService<DatabaseInitializationService>().InitialiseAsync();

        ConfigureLifetime();
        host.Start();
        SurfaceConfigurationErrors();
        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureLifetime()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = host.Services.GetRequiredService<MainWindow>();
            desktop.Exit += OnExit;
            desktop.MainWindow.Show();
        }
    }

    private void SurfaceConfigurationErrors()
        => ScrapeConfigurationValidator.Validate(host.Services.GetRequiredService<ScrapeConfiguration>())
            .Match(_ => Unit.Value, errors =>
            {
                var broadcaster = host.Services.GetRequiredService<LogBroadcaster>();

                foreach (var error in errors)
                    broadcaster.Broadcast($"Configuration error - {error.Property}: {error.Message}");

                return Unit.Value;
            });

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
        => host.StopAsync().GetAwaiter().GetResult();
}
