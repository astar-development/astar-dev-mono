using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb.ScrapeConfiguration;

/// <inheritdoc/>
/// <param name="context">The database context the projections run against.</param>
public sealed class ScrapeConfigurationLookup(ControlDbContext context) : IScrapeConfigurationLookup
{
    /// <inheritdoc/>
    public Task<Exceptional<IReadOnlyList<ScrapeConfigurationHeader>>> ListHeadersAsync(CancellationToken cancellationToken = default)
        => Try.RunAsync<IReadOnlyList<ScrapeConfigurationHeader>>(async () =>
        {
            var rows = await context.ScrapeConfigurations
                .AsNoTracking()
                .InFirstOrder()
                .Select(configuration => new { configuration.Id, configuration.BaseUrl, configuration.SearchConfiguration.SearchTerm })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return [.. rows.Select(row => new ScrapeConfigurationHeader(row.Id, row.BaseUrl, row.SearchTerm))];
        });

    /// <inheritdoc/>
    public Task<Exceptional<Option<string>>> TryGetRootDirectoryAsync(CancellationToken cancellationToken = default)
        => Try.RunAsync(async () =>
        {
            var rootDirectories = await context.ScrapeConfigurations
                .AsNoTracking()
                .InFirstOrder()
                .Select(configuration => configuration.ScrapeDirectories.RootDirectory)
                .Take(1)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return rootDirectories.Count == 0 ? Option.None<string>() : Option.Some(rootDirectories[0]);
        });

    /// <inheritdoc/>
    public Task<Exceptional<Option<RootDirectories>>> TryGetRootDirectoriesAsync(CancellationToken cancellationToken = default)
        => Try.RunAsync(async () =>
        {
            var rows = await context.ScrapeConfigurations
                .AsNoTracking()
                .InFirstOrder()
                .Select(configuration => new { configuration.ScrapeDirectories.RootDirectory, configuration.ScrapeDirectories.RootDirectoryFamous })
                .Take(1)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return rows.Count == 0 ? Option.None<RootDirectories>() : Option.Some(new RootDirectories(rows[0].RootDirectory, rows[0].RootDirectoryFamous));
        });
}
