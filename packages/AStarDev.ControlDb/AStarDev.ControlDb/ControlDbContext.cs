using AStarDev.ControlDb.FileDetail;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.ControlDb.TagDetail;
using AStarDev.EFCoreSqlite;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb;

/// <summary>Represents the Entity Framework database context and unit of work for the control database. Its repositories are resolved through <see cref="GetRepository{TAggregate, TKey}"/>.</summary>
/// <remarks>Initializes a new instance of the <see cref="ControlDbContext"/> class with the specified options.</remarks>
/// <param name="options">The options to be used by the DbContext.</param>
/// <param name="scrapeConfigurationQuery">The query used to auto-include the sub-entities required by a <see cref="ScrapeConfigurationEntity"/> aggregate.</param>
/// <param name="filesQuery">The query used to auto-include the sub-entities required by a <see cref="FileEntity"/> aggregate.</param>
public class ControlDbContext(DbContextOptions<ControlDbContext> options, IQuery<ScrapeConfigurationEntity>? scrapeConfigurationQuery = null, IQuery<FileEntity>? filesQuery = null)
    : DbContext(options), IUnitOfWork
{
    private readonly IQuery<ScrapeConfigurationEntity> scrapeConfigurationQuery = scrapeConfigurationQuery ?? new ScrapeConfigurationQuery();
    private readonly IQuery<FileEntity> filesQuery = filesQuery ?? new FileQuery();

    /// <inheritdoc/>
    public IRepository<TAggregate, TKey> GetRepository<TAggregate, TKey>() where TAggregate : IAggregateRoot
    => new object[] { new ScrapeConfigurationRepository(this, scrapeConfigurationQuery), new FileRepository(this, filesQuery), new TagRepository(this) }
        .OfType<IRepository<TAggregate, TKey>>()
        .ToList() is [var repository]
        ? repository
        : throw new InvalidOperationException($"There is no repository for aggregate {typeof(TAggregate).Name} with key {typeof(TKey).Name}.");

    /// <inheritdoc/>
    public async Task<T> InTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        var transaction = await Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using var disposal = transaction.ConfigureAwait(false);
        var result = await operation().ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return result;
    }

    /// <summary>Gets the repository for managing scrape configuration entities in the database.</summary>
    public DbSet<ScrapeConfigurationEntity> ScrapeConfigurations => Set<ScrapeConfigurationEntity>();

    public DbSet<FileEntity> Files => Set<FileEntity>();

    public DbSet<TagEntity> Tags => Set<TagEntity>();

    /// <summary>Gets the handles of the wallpapers that were ignored because of a tag flagged to ignore images.</summary>
    public DbSet<IgnoredWallpaperEntity> IgnoredWallpapers => Set<IgnoredWallpaperEntity>();

    public DbSet<FileTagEntity> FileTags => Set<FileTagEntity>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfigurationsFromAssembly(typeof(ControlDbContext).Assembly);

        modelBuilder.UseSqliteFriendlyConversions();
    }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        => configurationBuilder.Properties<string>().UseCollation("NOCASE");

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        if (!optionsBuilder.IsConfigured)
        {
            string tempDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _ = optionsBuilder.UseSqlite($"Data Source={tempDir}/Scraper/files.db");
        }

        _ = optionsBuilder
            .UseAsyncSeeding(async (context, _, cancellationToken) => await Seeder.SeedAsync(context, cancellationToken).ConfigureAwait(false))
            .UseSeeding((context, _) => Seeder.Seed(context));
    }
}
