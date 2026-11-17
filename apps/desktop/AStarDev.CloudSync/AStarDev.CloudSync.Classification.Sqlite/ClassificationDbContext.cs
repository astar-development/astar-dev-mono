using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.CloudSync.Classification.Sqlite;

/// <summary>The classification tables of the CloudSync SQLite database. It has its own migration history, apart from the accounts and sync ones.</summary>
/// <param name="options">The options that point the context at the database.</param>
[ExcludeFromCodeCoverage]
internal sealed class ClassificationDbContext(DbContextOptions<ClassificationDbContext> options) : DbContext(options)
{
    /// <summary>The name of this context's migration history table.</summary>
    internal const string MigrationsHistoryTable = "__ClassificationMigrationsHistory";

    /// <summary>The categories.</summary>
    public DbSet<CategoryEntity> Categories => Set<CategoryEntity>();

    /// <summary>The terms, ignored ones included.</summary>
    public DbSet<TermEntity> Terms => Set<TermEntity>();

    /// <summary>The links between files and terms.</summary>
    public DbSet<FileLinkEntity> FileLinks => Set<FileLinkEntity>();

    /// <summary>The seed state: at most one row.</summary>
    public DbSet<ClassificationStateEntity> State => Set<ClassificationStateEntity>();

    /// <summary>The last completed reclassify run: at most one row.</summary>
    public DbSet<ReclassifyRunEntity> ReclassifyRuns => Set<ReclassifyRunEntity>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CategoryEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TermEntityConfiguration());
        modelBuilder.ApplyConfiguration(new FileLinkEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ClassificationStateEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ReclassifyRunEntityConfiguration());
    }
}
