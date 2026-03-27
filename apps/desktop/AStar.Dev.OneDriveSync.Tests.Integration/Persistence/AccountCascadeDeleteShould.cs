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
        await using var context = await factory.CreateContextAsync();

        // Derive the real table name from the EF Core model so the test stays
        // consistent even if the naming convention changes.
        var accountTableName = context.Model
            .FindEntityType(typeof(Account))!
            .GetTableName();

        // Create a test-only stub child table that holds a FK to the accounts table.
        // This simulates any future entity that references Account via synthetic Guid FK.
        await context.Database.ExecuteSqlRawAsync($"""
            CREATE TABLE IF NOT EXISTS test_account_child (
                id          TEXT PRIMARY KEY NOT NULL,
                account_id  TEXT NOT NULL
                    REFERENCES {accountTableName} (id) ON DELETE CASCADE
            )
            """);

        var accountId = Guid.NewGuid();
        context.Accounts.Add(new Account
        {
            Id                 = accountId,
            DisplayName        = "Cascade Test User",
            Email              = "cascade@example.com",
            MicrosoftAccountId = "ms-cascade-001"
        });
        await context.SaveChangesAsync();

        var childId = Guid.NewGuid().ToString();
        await context.Database.ExecuteSqlRawAsync(
            $"INSERT INTO test_account_child (id, account_id) VALUES ('{childId}', '{accountId}')");

        // Verify the child row exists before the delete
        using (var checkCmd = factory.Connection.CreateCommand())
        {
            checkCmd.CommandText = $"SELECT COUNT(*) FROM test_account_child WHERE account_id = '{accountId}'";
            ((long?)checkCmd.ExecuteScalar()).ShouldBe(1);
        }

        // Act
        var account = await context.Accounts.FindAsync(accountId);
        account.ShouldNotBeNull();
        context.Accounts.Remove(account);
        await context.SaveChangesAsync();

        // Assert — no orphaned rows
        using var assertCmd = factory.Connection.CreateCommand();
        assertCmd.CommandText = $"SELECT COUNT(*) FROM test_account_child WHERE account_id = '{accountId}'";
        var remaining = (long?)assertCmd.ExecuteScalar();
        remaining.ShouldBe(0);
    }

    [Fact]
    public async Task NotLeaveOrphanedRowsWhenMultipleAccountsExistAndOneIsDeleted()
    {
        // Arrange — two accounts, two child rows; only one account deleted
        await using var factory = AppDbContextFactory.Create();
        await using var context = await factory.CreateContextAsync();

        var accountTableName = context.Model
            .FindEntityType(typeof(Account))!
            .GetTableName();

        await context.Database.ExecuteSqlRawAsync($"""
            CREATE TABLE IF NOT EXISTS test_account_child (
                id          TEXT PRIMARY KEY NOT NULL,
                account_id  TEXT NOT NULL
                    REFERENCES {accountTableName} (id) ON DELETE CASCADE
            )
            """);

        var keptAccountId    = Guid.NewGuid();
        var deletedAccountId = Guid.NewGuid();

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
        await context.SaveChangesAsync();

        await context.Database.ExecuteSqlRawAsync(
            $"INSERT INTO test_account_child (id, account_id) VALUES ('{Guid.NewGuid()}', '{keptAccountId}')");
        await context.Database.ExecuteSqlRawAsync(
            $"INSERT INTO test_account_child (id, account_id) VALUES ('{Guid.NewGuid()}', '{deletedAccountId}')");

        // Act
        var toDelete = await context.Accounts.FindAsync(deletedAccountId);
        toDelete.ShouldNotBeNull();
        context.Accounts.Remove(toDelete);
        await context.SaveChangesAsync();

        // Assert — the kept account's child row still exists; the deleted account's child is gone
        using var cmd = factory.Connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM test_account_child WHERE account_id = '{deletedAccountId}'";
        ((long?)cmd.ExecuteScalar()).ShouldBe(0);

        using var cmd2 = factory.Connection.CreateCommand();
        cmd2.CommandText = $"SELECT COUNT(*) FROM test_account_child WHERE account_id = '{keptAccountId}'";
        ((long?)cmd2.ExecuteScalar()).ShouldBe(1);
    }
}
