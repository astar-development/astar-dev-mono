using Microsoft.EntityFrameworkCore;

namespace AStarDev.EFCoreExtensions.TestsUnit.Support;

/// <summary>Maps every entity using the extensions' default (SQLite) provider.</summary>
internal sealed class SqliteDefaultContext(DbContextOptions<SqliteDefaultContext> options) : DbContext(options)
{
    public DbSet<IntEntity> Ints => Set<IntEntity>();
    public DbSet<LongEntity> Longs => Set<LongEntity>();
    public DbSet<GuidEntity> Guids => Set<GuidEntity>();
    public DbSet<StringEntity> Strings => Set<StringEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IntEntity>().HasGeneratedIntId(e => e.Id);
        modelBuilder.Entity<LongEntity>().HasGeneratedLongId(e => e.Id);
        modelBuilder.Entity<GuidEntity>().HasGeneratedGuidId(e => e.Id);
        modelBuilder.Entity<StringEntity>().HasStringId(e => e.Id);
    }
}

/// <summary>Maps int/long entities asking for a non-SQLite provider strategy.</summary>
internal sealed class OtherProviderContext(DbContextOptions<OtherProviderContext> options) : DbContext(options)
{
    public DbSet<IntEntity> Ints => Set<IntEntity>();
    public DbSet<LongEntity> Longs => Set<LongEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IntEntity>().HasGeneratedIntId(e => e.Id, DatabaseProvider.SqlServer);
        modelBuilder.Entity<LongEntity>().HasGeneratedLongId(e => e.Id, DatabaseProvider.PostgreSql);
    }
}
