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
    ///     Gets or sets the File Classifications
    /// </summary>
    public DbSet<FileClassification> FileClassifications { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the DuplicatesDetails loaded from the configured view in the database
    /// </summary>
    public DbSet<DuplicatesDetails> DuplicatesDetails { get; set; } = null!;

    /// <summary>
    ///     The overridden OnModelCreating method
    /// </summary>
    /// <param name="modelBuilder">
    /// </param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.UseCollation("SQL_Latin1_General_CP1_CI_AS");
        _ = modelBuilder.HasDefaultSchema(Constants.SchemaName);
        _ = modelBuilder.ApplyConfigurationsFromAssembly(typeof(FilesContext).Assembly);

        _ = modelBuilder
            .Entity<DuplicatesDetails>(eb =>
                                       {
                                           _ = eb.HasNoKey();
                                           _ = eb.ToView("vw_DuplicatesDetails");
                                       });
    }
}