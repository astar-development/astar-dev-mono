using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.EFCoreExtensions.TestsUnit.Support;

/// <summary>Owns an open in-memory SQLite connection and creates fresh contexts sharing it.</summary>
internal sealed class SqliteFixture<TContext> : IDisposable
    where TContext : DbContext
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly DbContextOptions<TContext> options;

    public SqliteFixture()
    {
        connection.Open();
        options = new DbContextOptionsBuilder<TContext>().UseSqlite(connection).Options;
        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public TContext CreateContext() => (TContext)Activator.CreateInstance(typeof(TContext), options)!;

    public void Dispose() => connection.Dispose();
}
