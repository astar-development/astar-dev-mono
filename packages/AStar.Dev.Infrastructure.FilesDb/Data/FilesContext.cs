using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Infrastructure.FilesDb.Data;

/// <summary>
///     The <see cref="FilesContext" /> class
/// </summary>
/// <remarks>
///     The list of files in the dB
/// </remarks>
public sealed class FilesContext : DbContext
{
    /// <summary>
    /// </summary>
    /// <param name="options"></param>
    public FilesContext(DbContextOptions<FilesContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// </summary>
    public FilesContext()
        : base(new DbContextOptions<FilesContext>())
    {
    }

    /// <summary>
    ///     The list of files in the dB
    /// </summary>
    public DbSet<FileDetail> Files { get; set; } = null!;

    /// <summary>
    ///     The list of file access details in the dB
    /// </summary>
    public DbSet<FileAccessDetail> FileAccessDetails { get; set; } = null!;

    /// <summary>
    /// </summary>
    public DbSet<FileNamePart> FileNameParts { get; set; } = null!;

    /// <summary>
    ///     The list of tags to ignore
    /// </summary>
    public DbSet<TagToIgnore> TagsToIgnore { get; set; } = null!;

    /// <summary>
    ///     The list of models to ignore completely
    /// </summary>
    public DbSet<ModelToIgnore> ModelsToIgnore { get; set; } = null!;

    /// <summary>
    ///    Gets or sets the Scrape Configuration details for the scraper, including connection strings, user configuration, search configuration, and scrape directories. This property serves as a central point for managing all the necessary settings and parameters that the scraper needs to operate effectively. The ScrapeConfiguration property allows for flexible configuration of different aspects of the scraping process, enabling the scraper to adapt to various environments and requirements without hardcoding sensitive information directly into the codebase. Proper management of scrape configuration is crucial for ensuring that the scraper can operate efficiently and securely, allowing it to access necessary resources and perform its tasks without issues.
    /// </summary>
    public DbSet<ScrapeConfigurationEntity> ScrapeConfiguration { get; set; } = null!;

    /// <summary>
    ///    Gets or sets the search configuration table.
    /// </summary>
    public DbSet<SearchConfiguration> SearchConfigurations { get; set; } = null!;

    /// <summary>
    ///     Gets or sets all unique tags observed during scraping
    /// </summary>
    public DbSet<ScrapedTag> ScrapedTags { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the File Classifications
    /// </summary>
    public DbSet<FileClassification> FileClassifications { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the classifications applied to files at download time
    /// </summary>
    public DbSet<DownloadedFileClassification> DownloadedFileClassifications { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if(!optionsBuilder.IsConfigured) _ = optionsBuilder.UseSqlite("Data Source=/home/jbarden/Documents/Scrapper/files.db");
    }

    /// <summary>
    ///     The overridden OnModelCreating method
    /// </summary>
    /// <param name="modelBuilder">
    /// </param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseSqliteFriendlyConversions();
        _ = modelBuilder.UseCollation("SQL_Latin1_General_CP1_CI_AS");
        _ = modelBuilder.HasDefaultSchema(Constants.SchemaName);
        _ = modelBuilder.ApplyConfigurationsFromAssembly(typeof(FilesContext).Assembly);

        modelBuilder
           .Entity<DuplicatesDetails>(eb =>
                                      {
                                          eb.HasNoKey();
                                          eb.ToView("vw_DuplicatesDetails");
                                      });
    }
}
