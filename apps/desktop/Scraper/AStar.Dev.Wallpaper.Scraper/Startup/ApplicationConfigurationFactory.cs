using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;

namespace AStar.Dev.Wallpaper.Scraper.Startup;

/// <summary>Builds the application's configuration root from appsettings.json, user secrets, and environment variables.</summary>
[ExcludeFromCodeCoverage]
public static class ApplicationConfigurationFactory
{
    /// <summary>Builds the configuration root, reading appsettings.json from <paramref name="basePath" /> and layering user secrets and environment variables on top.</summary>
    /// <param name="basePath">The directory containing appsettings.json.</param>
    public static IConfigurationRoot Build(string basePath) =>
        new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddUserSecrets<App>(optional: true)
            .AddEnvironmentVariables()
            .Build();
}
