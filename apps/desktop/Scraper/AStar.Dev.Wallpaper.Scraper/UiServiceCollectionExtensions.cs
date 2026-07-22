using AStar.Dev.Wallpaper.Scraper.Configuration.EntityEditor;
using AStar.Dev.Wallpaper.Scraper.Home;
using AStar.Dev.Wallpaper.Scraper.Services;
using AStar.Dev.Wallpaper.Scraper.Theming;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.Wallpaper.Scraper;

/// <summary>Registers the update checker and UI-facing services with the dependency injection container.</summary>
public static class UiServiceCollectionExtensions
{
    /// <summary>Registers the update service, main window, its view model, and the entity editor factory.</summary>
    /// <param name="services">The service collection to register the UI services with.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddUiServices(this IServiceCollection services)
    {
        services.AddSingleton<UpdateService>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();
        services.AddSingleton<IEntityEditorFactory, EntityEditorFactory>();
        services.AddSingleton<IThemeService, ThemeService>();

        return services;
    }
}
