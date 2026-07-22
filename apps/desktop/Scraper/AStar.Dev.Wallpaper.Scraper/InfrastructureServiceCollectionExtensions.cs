using System.IO.Abstractions;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Wallpaper.Scraper.Configuration;
using AStar.Dev.Wallpaper.Scraper.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Testably.Abstractions;

namespace AStar.Dev.Wallpaper.Scraper;

/// <summary>Registers file system, database, and clock infrastructure services with the dependency injection container.</summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Registers the file system abstraction, the database context factory, and the system clock delegate.</summary>
    /// <param name="services">The service collection to register the infrastructure services with.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<IFileSystem, RealFileSystem>();
        services.AddDbContextFactory<AppDbContext>((serviceProvider, options) =>
            options.UseSqlite(serviceProvider.GetRequiredService<IOptions<ScrapeConfiguration>>().Value.ConnectionStrings.Sqlite));
        services.AddTransient<Clock>(serviceProvider => () => DateTimeOffset.Now);

        return services;
    }
}
