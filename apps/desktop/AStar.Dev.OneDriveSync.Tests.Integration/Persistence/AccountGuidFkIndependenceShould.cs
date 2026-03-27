using AStar.Dev.OneDriveSync.Accounts;
using AStar.Dev.OneDriveSync.Tests.Integration.Helpers;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDriveSync.Tests.Integration.Persistence;

/// <summary>
///     Verifies AC: "the synthetic account <see cref="Guid" /> FK is independent of any
///     Microsoft account detail — changing <c>MicrosoftAccountId</c> does not affect FK
///     relationships" (S002 — Data Integrity Tests).
///
///     The synthetic <see cref="Guid" /> primary key (<c>Account.Id</c>) is the sole FK
///     target for every other table.  Rotating, invalidating, or changing an account's
///     Microsoft identity detail must not alter the PK value that ties the account to all
///     its related data.
///
///     Note on Guid format: EF Core 10 stores <see cref="Guid" /> values as uppercase TEXT
///     in SQLite.  Raw ADO.NET SQL uses <c>guid.ToString("D").ToUpperInvariant()</c> to match, because
///     SQLite TEXT comparisons are case-sensitive.
/// </summary>
public sealed class AccountGuidFkIndependenceShould
{
    [Fact]
    public async Task PreserveAccountGuidWhenMicrosoftAccountIdChanges()
    {
        // Arrange
        await using var factory = AppDbContextFactory.Create();
        await using var context = await factory.CreateContextAsync(TestContext.Current.CancellationToken);

        var originalGuid = Guid.NewGuid();
        context.Accounts.Add(new Account
        {
            Id                 = originalGuid,
            DisplayName        = "FK Independence User",
            Email              = "fk@example.com",
            MicrosoftAccountId = "original-ms-id"
        });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act — change the Microsoft identity detail
        var account = await context.Accounts.FindAsync(new object?[] { originalGuid }, TestContext.Current.CancellationToken);
        account.ShouldNotBeNull();
        account.MicrosoftAccountId = "rotated-ms-id";
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert — the synthetic Guid PK is unchanged
        var reloaded = await context.Accounts.FindAsync(new object?[] { originalGuid }, TestContext.Current.CancellationToken);
        reloaded.ShouldNotBeNull();
        reloaded.Id.ShouldBe(originalGuid);
        reloaded.MicrosoftAccountId.ShouldBe("rotated-ms-id");
    }

    [Fact]
    public async Task PreserveLinkedChildRowsWhenMicrosoftAccountIdChanges()
    {
        // Arrange — a child row is linked to the account via synthetic Guid FK.
        // Changing MicrosoftAccountId must not break the FK relationship.
        await using var factory = AppDbContextFactory.Create();
        await using var context = await factory.CreateContextAsync(TestContext.Current.CancellationToken);

        var accountTableName = context.Model
            .FindEntityType(typeof(Account))!
            .GetTableName();

        // SQL assigned to variable to avoid EF1002 (ExecuteSqlRawAsync with inline interpolated string).
        var createChildTableSql = $"""
            CREATE TABLE IF NOT EXISTS test_ms_id_child (
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
            DisplayName        = "Linked User",
            Email              = "linked@example.com",
            MicrosoftAccountId = "ms-original"
        });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Uppercase to match EF Core's stored Guid TEXT format (SQLite TEXT is case-sensitive).
        var childId        = Guid.NewGuid().ToString();
        var accountIdUpper = accountId.ToString("D").ToUpperInvariant();
        var insertChildSql = $"INSERT INTO test_ms_id_child (id, account_id) VALUES ('{childId}', '{accountIdUpper}')";
        await context.Database.ExecuteSqlRawAsync(insertChildSql, TestContext.Current.CancellationToken);

        // Act — rotate the Microsoft account ID
        var account = await context.Accounts.FindAsync(new object?[] { accountId }, TestContext.Current.CancellationToken);
        account.ShouldNotBeNull();
        account.MicrosoftAccountId = "ms-rotated";
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert — child row still references the account via the unchanged Guid FK
        using var cmd = factory.Connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM test_ms_id_child WHERE account_id = '{accountIdUpper}'";
        var count = (long?)cmd.ExecuteScalar();
        count.ShouldBe(1);
    }

    [Fact]
    public async Task AssignDifferentGuidToEachAccountRegardlessOfMicrosoftAccountId()
    {
        // Two accounts with the same MicrosoftAccountId must have distinct synthetic Guids.
        // (Edge-case: account re-creation after deletion.)
        await using var factory = AppDbContextFactory.Create();
        await using var context = await factory.CreateContextAsync(TestContext.Current.CancellationToken);

        var firstGuid  = Guid.NewGuid();
        var secondGuid = Guid.NewGuid();

        context.Accounts.AddRange(
            new Account
            {
                Id                 = firstGuid,
                DisplayName        = "First",
                Email              = "first@example.com",
                MicrosoftAccountId = "shared-ms-id"
            },
            new Account
            {
                Id                 = secondGuid,
                DisplayName        = "Second",
                Email              = "second@example.com",
                MicrosoftAccountId = "shared-ms-id"
            });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert — each account has its own independent Guid
        firstGuid.ShouldNotBe(secondGuid);

        var first  = await context.Accounts.FindAsync(new object?[] { firstGuid },  TestContext.Current.CancellationToken);
        var second = await context.Accounts.FindAsync(new object?[] { secondGuid }, TestContext.Current.CancellationToken);
        first.ShouldNotBeNull();
        second.ShouldNotBeNull();
        first.Id.ShouldNotBe(second.Id);
    }
}
