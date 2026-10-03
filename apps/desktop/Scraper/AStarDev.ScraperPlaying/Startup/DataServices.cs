using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.Utilities;
using AStarDev.ScraperPlaying.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.Startup;

public static class DataServices
{
    /// <summary>Registers the data services against the real per-user application data database.</summary>
    /// <param name="services">The service collection to register the data services with.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddDataServices(this IServiceCollection services) => services.AddDataServices(BuildDbPath());

    /// <summary>Registers the data services against the SQLite database at <paramref name="databasePath" />, so tests can point at a temp file instead of the real user database.</summary>
    /// <param name="services">The service collection to register the data services with.</param>
    /// <param name="databasePath">The SQLite database file path.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddDataServices(this IServiceCollection services, string databasePath)
    {
        // The queries are stateless and must be singletons: the DbContext factory is a singleton and builds ControlDbContext from the root provider.
        _ = services.AddSingleton<IQuery<ScrapeConfigurationEntity>, ScrapeConfigurationQuery>()
            .AddSingleton<IQuery<FileEntity>, FileQuery>()
            .AddScoped<IUnitOfWork, ControlDbContext>()
            .AddScoped<IScrapeConfigurationLookup, ScrapeConfigurationLookup>()
            .AddScoped<IFilesQuery, FilesQuery>()
            .AddScoped<IFileDetailsClearer, FileDetailsClearer>()
            .AddScoped<IIgnoredWallpapers, IgnoredWallpapers>()
            .AddScoped<ITagsQuery, TagsQuery>()
            .AddScoped<IFileTagRepository, FileTagRepository>()
            .AddDbContextFactory<ControlDbContext>((serviceProvider, options) => options.UseSqlite($"Data Source={databasePath}"))
            // AddDbContextFactory also registers ControlDbContext itself as its own scoped service, independent
            // of the IUnitOfWork registration above - within one scope that would otherwise resolve a *second*,
            // never-saved ControlDbContext instance for anything (FilesQuery, TagsQuery, FileTagRepository) that
            // takes ControlDbContext directly. Registering it last makes every direct ControlDbContext resolution
            // reuse the same instance IUnitOfWork resolves.
            .AddScoped(serviceProvider => (ControlDbContext)serviceProvider.GetRequiredService<IUnitOfWork>());

        return services;
    }

    private static string BuildDbPath()
        => Path.Combine(ApplicationMetadata.ApplicationNameHyphenated.ApplicationDirectory(), "data", "astar-control.db");
}
