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
///
///     Note on Guid format: EF Core 10 stores <see cref="Guid" /> values as uppercase TEXT
///     (e.g. <c>1B7150A8-83C4-…</c>).  Raw ADO.NET SQL must use
///     <c>guid.ToString("D").ToUpperInvariant()</c> to match, because SQLite TEXT comparisons are
///     case-sensitive.
/// </summary>
public sealed class AccountCascadeDeleteShould
{
    [Fact]
    public async Task RemoveAllLinkedRowsWhenAccountIsDeleted()
    {
        // Arrange
        await using var factory = AppDbContextFactory.Create();
        await using var context = await factory.CreateContextAsync(TestContext.Current.CancellationToken);

        // Derive the real table name from the EF Core model so the test stays
        // consistent even if the naming convention changes.
        var accountTableName = context.Model
            .FindEntityType(typeof(Account))!
            .GetTableName();

        // SQL is assigned to a variable to avoid EF1002 (ExecuteSqlRawAsync with inline
        // interpolated string). The table name is safe — it comes from EF Core model metadata.
        var createChildTableSql = $"""
            CREATE TABLE IF NOT EXISTS test_account_child (
                id          TEXT PRIMARY KEY NOT NULL,
                account_id  TEXT NOT NULL
                    REFERENCES {accountTableName} (id) ON DELETE CASCADE
            )
            """;
        await context.Database.ExecuteSqlRawAsync(createChildTableSql, TestContext.Current.CancellationToken);

        var accountId = Guid.NewGuid();
        context.Accounts.Add(new Account
        {
            Id                 = accountId,
            DisplayName        = "Cascade Test User",
            Email              = "cascade@example.com",
            MicrosoftAccountId = "ms-cascade-001"
        });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // EF Core 10 stores Guids as uppercase TEXT in SQLite.  Use ToUpper() so the FK
        // constraint and the subsequent COUNT query both reference the same stored format.
        var childId           = Guid.NewGuid().ToString();
        var accountIdUpperSql = accountId.ToString("D").ToUpperInvariant();
        var insertChildSql    = $"INSERT INTO test_account_child (id, account_id) VALUES ('{childId}', '{accountIdUpperSql}')";
        await context.Database.ExecuteSqlRawAsync(insertChildSql, TestContext.Current.CancellationToken);

        // Verify the child row exists before the delete
        using (var checkCmd = factory.Connection.CreateCommand())
        {
            checkCmd.CommandText = $"SELECT COUNT(*) FROM test_account_child WHERE account_id = '{accountIdUpperSql}'";
            ((long?)checkCmd.ExecuteScalar()).ShouldBe(1);
        }

        // Act
        var account = await context.Accounts.FindAsync(new object?[] { accountId }, TestContext.Current.CancellationToken);
        account.ShouldNotBeNull();
        context.Accounts.Remove(account);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert — no orphaned rows
        using var assertCmd = factory.Connection.CreateCommand();
        assertCmd.CommandText = $"SELECT COUNT(*) FROM test_account_child WHERE account_id = '{accountIdUpperSql}'";
        var remaining = (long?)assertCmd.ExecuteScalar();
        remaining.ShouldBe(0);
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

        // Uppercase to match EF Core's stored Guid TEXT format.
        var keptIdUpper    = keptAccountId.ToString("D").ToUpperInvariant();
        var deletedIdUpper = deletedAccountId.ToString("D").ToUpperInvariant();
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
        using var cmd = factory.Connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM test_account_child WHERE account_id = '{deletedIdUpper}'";
        ((long?)cmd.ExecuteScalar()).ShouldBe(0);

        using var cmd2 = factory.Connection.CreateCommand();
        cmd2.CommandText = $"SELECT COUNT(*) FROM test_account_child WHERE account_id = '{keptIdUpper}'";
        ((long?)cmd2.ExecuteScalar()).ShouldBe(1);
    }
}
