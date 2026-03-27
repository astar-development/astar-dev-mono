using AStar.Dev.OneDriveSync.Accounts;
using AStar.Dev.OneDriveSync.Tests.Integration.Helpers;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDriveSync.Tests.Integration.Persistence;

/// <summary>
///     Verifies AC: "account deletion removes all rows referencing that account across every
///     table; no orphaned rows remain" (S002 — Data Integrity Tests).
///
///     A test-only stub table (<c>test_account_child</c>) is created in raw SQL with
///     <c>ON DELETE CASCADE</c> to validate the SQLite FK cascade mechanism that the
///     production <see cref="AppDbContext" /> configuration must enable.  Real child
///     entities added in later stories will extend these tests in-place.
/// </summary>
public sealed class AccountCascadeDeleteShould
{
    [Fact]
    public async Task RemoveAllLinkedRowsWhenAccountIsDeleted()
    {
        // Arrange
        await using var factory = AppDbContextFactory.Create();
        await using var context = await factory.CreateContextAsync(TestContext.Current.CancellationToken);

        var accountTableName = context.Model
            .FindEntityType(typeof(Account))!
            .GetTableName();

        var createChildTableSql = $"""
            CREATE TABLE IF NOT EXISTS test_account_child (
                id          TEXT PRIMARY KEY NOT NULL,
                account_id  TEXT NOT NULL
                    REFERENCES {accountTableName} (id) ON DELETE CASCADE
            )
            """;
        await context.Database.ExecuteSqlRawAsync(createChildTableSql, TestContext.Current.CancellationToken);

        var accountId      = Guid.NewGuid();
        var accountIdUpper = accountId.ToString("D").ToUpperInvariant();
        context.Accounts.Add(new Account
        {
            Id                 = accountId,
            DisplayName        = "Cascade Test User",
            Email              = "cascade@example.com",
            MicrosoftAccountId = "ms-cascade-001"
        });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var insertChildSql = $"INSERT INTO test_account_child (id, account_id) VALUES ('{Guid.NewGuid()}', '{accountIdUpper}')";
        await context.Database.ExecuteSqlRawAsync(insertChildSql, TestContext.Current.CancellationToken);

        SqliteAssert.ChildRowCount(factory.Connection, "test_account_child", accountId, expected: 1);

        // Act
        var account = await context.Accounts.FindAsync(new object?[] { accountId }, TestContext.Current.CancellationToken);
        account.ShouldNotBeNull();
        context.Accounts.Remove(account);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        SqliteAssert.ChildRowCount(factory.Connection, "test_account_child", accountId, expected: 0);
    }

    [Fact]
    public async Task NotLeaveOrphanedRowsWhenMultipleAccountsExistAndOneIsDeleted()
    {
        // Arrange — two accounts, two child rows; only one account deleted
        await using var factory = AppDbContextFactory.Create();
        await using var context = await factory.CreateContextAsync(TestContext.Current.CancellationToken);

        var accountTableName = context.Model
            .FindEntityType(typeof(Account))!
            .GetTableName();

        var createChildTableSql = $"""
            CREATE TABLE IF NOT EXISTS test_account_child (
                id          TEXT PRIMARY KEY NOT NULL,
                account_id  TEXT NOT NULL
                    REFERENCES {accountTableName} (id) ON DELETE CASCADE
            )
            """;
        await context.Database.ExecuteSqlRawAsync(createChildTableSql, TestContext.Current.CancellationToken);

        var keptAccountId    = Guid.NewGuid();
        var deletedAccountId = Guid.NewGuid();
        var keptIdUpper      = keptAccountId.ToString("D").ToUpperInvariant();
        var deletedIdUpper   = deletedAccountId.ToString("D").ToUpperInvariant();

        context.Accounts.AddRange(
            new Account
            {
                Id                 = keptAccountId,
                DisplayName        = "Keeper",
                Email              = "keep@example.com",
                MicrosoftAccountId = "ms-keep-001"
            },
            new Account
            {
                Id                 = deletedAccountId,
                DisplayName        = "To Delete",
                Email              = "delete@example.com",
                MicrosoftAccountId = "ms-delete-001"
            });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var insertKeptSql    = $"INSERT INTO test_account_child (id, account_id) VALUES ('{Guid.NewGuid()}', '{keptIdUpper}')";
        var insertDeletedSql = $"INSERT INTO test_account_child (id, account_id) VALUES ('{Guid.NewGuid()}', '{deletedIdUpper}')";
        await context.Database.ExecuteSqlRawAsync(insertKeptSql,    TestContext.Current.CancellationToken);
        await context.Database.ExecuteSqlRawAsync(insertDeletedSql, TestContext.Current.CancellationToken);

        // Act
        var toDelete = await context.Accounts.FindAsync(new object?[] { deletedAccountId }, TestContext.Current.CancellationToken);
        toDelete.ShouldNotBeNull();
        context.Accounts.Remove(toDelete);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert — the kept account's child row still exists; the deleted account's child is gone
        SqliteAssert.ChildRowCount(factory.Connection, "test_account_child", deletedAccountId, expected: 0);
        SqliteAssert.ChildRowCount(factory.Connection, "test_account_child", keptAccountId,    expected: 1);
    }
}
