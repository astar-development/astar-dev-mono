using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AStarDev.CloudSync.Classification.Sqlite;

/// <summary>Lets <c>dotnet ef</c> create the context when it adds migrations. The app never uses it.</summary>
[ExcludeFromCodeCoverage]
internal sealed class ClassificationDesignTimeDbContextFactory : IDesignTimeDbContextFactory<ClassificationDbContext>
{
    /// <inheritdoc />
    public ClassificationDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<ClassificationDbContext>().UseSqlite("Data Source=design-time.db", sqlite => sqlite.MigrationsHistoryTable(ClassificationDbContext.MigrationsHistoryTable)).Options);
}
