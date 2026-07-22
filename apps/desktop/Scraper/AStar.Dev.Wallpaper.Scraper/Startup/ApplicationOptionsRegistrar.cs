using System.Diagnostics.CodeAnalysis;
using AStar.Dev.Wallpaper.Scraper.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AStar.Dev.Wallpaper.Scraper.Startup;

/// <summary>Registers strongly-typed options that must be bound and validated during application startup.</summary>
[ExcludeFromCodeCoverage]
public static class ApplicationOptionsRegistrar
{
    /// <summary>Binds <see cref="SyncSettings" /> from configuration and validates it via data annotations on start.</summary>
    /// <param name="services">The service collection to register options into.</param>
    /// <param name="configuration">The configuration containing the <see cref="SyncSettings.SectionName" /> section.</param>
    public static void Register(IServiceCollection services, IConfiguration configuration) =>
        _ = services.AddOptions<SyncSettings>()
                .Bind(configuration.GetSection(SyncSettings.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
}
