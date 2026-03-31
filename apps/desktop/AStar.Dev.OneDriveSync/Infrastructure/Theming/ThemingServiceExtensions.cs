using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDriveSync.Infrastructure.Theming;

/// <summary>Registers all theming services.</summary>
internal static class ThemingServiceExtensions
{
    /// <summary>Adds <see cref="IThemeService" /> and its dependencies to <paramref name="services" />.</summary>
    public static IServiceCollection AddTheming(this IServiceCollection services)
    {
        _ = services.AddSingleton<IApplicationThemeAdapter, AvaloniaApplicationThemeAdapter>();
        _ = services.AddSingleton<IPlatformThemeProvider, AvaloniaPlatformThemeProvider>();
        _ = services.AddSingleton<IAppSettingsRepository, AppSettingsRepository>();
        _ = services.AddSingleton<IThemeService, ThemeService>();

        return services;
    }
}
