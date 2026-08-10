using ImmutableDomain.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.FilesDb;

/// <summary>
/// Represents the Entity Framework database context for managing file entities.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="FilesDbContext"/> class with the specified options.
/// </remarks>
/// <param name="options">The options to be used by the DbContext.</param>
public class FilesDbContext(DbContextOptions<FilesDbContext> options) : DbContext(options)
{
    /// <summary>
    ///   Gets the repository for managing file entities in the database.
    /// </summary>
    public FilesDbContext() : this(new DbContextOptions<FilesDbContext>()) {}

    /// <summary>
    /// Gets the repository for managing file entities in the database.
    /// </summary>
    public IImmutableEntityRepository<FileEntity> FilesRepository => Set<FileEntity>().ToImmutableEntityRepository(this);

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FileEntity>().Property<FileId>("Id").HasConversion(fileId => fileId.Value, value => new FileId(value)).ValueGeneratedOnAdd();
        modelBuilder.Entity<FileEntity>().HasKey("Id");

        _ = modelBuilder.ApplyConfigurationsFromAssembly(typeof(FilesDbContext).Assembly);

        modelBuilder.UseSqliteFriendlyConversions();
    }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        => configurationBuilder.Properties<string>().UseCollation("NOCASE");

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        string tempDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!optionsBuilder.IsConfigured) _ = optionsBuilder.UseSqlite($"Data Source={tempDir}/Scraper/files.db");

        optionsBuilder
            .UseAsyncSeeding(async (context, _, cancellationToken) =>
            {
                // do some async seeding here if needed
                Console.WriteLine("Seeding FilesDbContext... this line is just for demonstration purposes and should be removed.");

                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            })
            .UseSeeding((context, _) =>
            {
                // do some seeding here if needed
                Console.WriteLine("Seeding FilesDbContext... this line is just for demonstration purposes and should be removed.");
                context.SaveChanges();
            });
    }
}

