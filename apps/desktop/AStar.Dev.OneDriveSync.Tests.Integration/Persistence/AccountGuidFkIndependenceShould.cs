using AStar.Dev.OneDriveSync.Accounts;
using AStar.Dev.OneDriveSync.Tests.Integration.Helpers;

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
/// </summary>
public sealed class AccountGuidFkIndependenceShould
{
    [Fact]
    public async Task PreserveAccountGuidWhenMicrosoftAccountIdChanges()
    {
        // Arrange
        await using var factory = AppDbContextFactory.Create();
        await using var context = await factory.CreateContextAsync();

        var originalGuid = Guid.NewGuid();
        context.Accounts.Add(new Account
        {
            Id                 = originalGuid,
            DisplayName        = "FK Independence User",
            Email              = "fk@example.com",
            MicrosoftAccountId = "original-ms-id"
        });
        await context.SaveChangesAsync();

        // Act — change the Microsoft identity detail
        var account = await context.Accounts.FindAsync(originalGuid);
        account.ShouldNotBeNull();
        account.MicrosoftAccountId = "rotated-ms-id";
        await context.SaveChangesAsync();

        // Assert — the synthetic Guid PK is unchanged
        var reloaded = await context.Accounts.FindAsync(originalGuid);
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
        await using var context = await factory.CreateContextAsync();

        var accountTableName = context.Model
            .FindEntityType(typeof(Account))!
            .GetTableName();

        await context.Database.ExecuteSqlRawAsync($"""
            CREATE TABLE IF NOT EXISTS test_ms_id_child (
                id          TEXT PRIMARY KEY NOT NULL,
                account_id  TEXT NOT NULL
                    REFERENCES {accountTableName} (id) ON DELETE CASCADE
            )
            """);

        var accountId = Guid.NewGuid();
        context.Accounts.Add(new Account
        {
            Id                 = accountId,
            DisplayName        = "Linked User",
            Email              = "linked@example.com",
            MicrosoftAccountId = "ms-original"
        });
        await context.SaveChangesAsync();

        var childId = Guid.NewGuid().ToString();
        await context.Database.ExecuteSqlRawAsync(
            $"INSERT INTO test_ms_id_child (id, account_id) VALUES ('{childId}', '{accountId}')");

        // Act — rotate the Microsoft account ID
        var account = await context.Accounts.FindAsync(accountId);
        account.ShouldNotBeNull();
        account.MicrosoftAccountId = "ms-rotated";
        await context.SaveChangesAsync();

        // Assert — child row still references the account via the unchanged Guid FK
        using var cmd = factory.Connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM test_ms_id_child WHERE account_id = '{accountId}'";
        var count = (long?)cmd.ExecuteScalar();
        count.ShouldBe(1);
    }

    [Fact]
    public async Task AssignDifferentGuidToEachAccountRegardlessOfMicrosoftAccountId()
    {
        // Two accounts with the same MicrosoftAccountId must have distinct synthetic Guids.
        // (Edge-case: account re-creation after deletion.)
        await using var factory = AppDbContextFactory.Create();
        await using var context = await factory.CreateContextAsync();

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
        await context.SaveChangesAsync();

        // Assert — each account has its own independent Guid
        firstGuid.ShouldNotBe(secondGuid);

        var first  = await context.Accounts.FindAsync(firstGuid);
        var second = await context.Accounts.FindAsync(secondGuid);
        first.ShouldNotBeNull();
        second.ShouldNotBeNull();
        first.Id.ShouldNotBe(second.Id);
    }
}
