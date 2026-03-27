using AStar.Dev.OneDriveSync.Features.Accounts;
using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDriveSync.Tests.Integration.Helpers;

internal static class AppDbContextExtensions
{
    public static async Task CreateStubChildTableAsync(this AppDbContext context, string tableName, bool cascadeOnDelete = true, CancellationToken cancellationToken = default)
    {
        var accountTableName = context.Model.FindEntityType(typeof(Account))!.GetTableName();
        var onDelete = cascadeOnDelete ? "ON DELETE CASCADE" : string.Empty;

        await context.Database.ExecuteSqlRawAsync($"""
            CREATE TABLE IF NOT EXISTS {tableName} (
                id          TEXT PRIMARY KEY NOT NULL,
                account_id  TEXT NOT NULL
                    REFERENCES {accountTableName} (id) {onDelete}
            )
            """, cancellationToken).ConfigureAwait(false);
    }
}
