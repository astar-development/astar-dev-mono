using AStar.Dev.OneDriveSync.Accounts;
using AStar.Dev.OneDriveSync.Tests.Integration.Helpers;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDriveSync.Tests.Integration.Persistence;

/// <summary>
///     Verifies AC: "inserting a row into any non-Account table with a real email/name
///     instead of a synthetic <see cref="Guid" /> FK fails at the schema level" (S002).
///
///     PII isolation is enforced by verifying — via EF Core model metadata — that
///     <c>Email</c>, <c>DisplayName</c>, and <c>MicrosoftAccountId</c> properties
///     exist only on the <see cref="Account" /> entity.  Any new entity added in future
///     stories that accidentally exposes PII columns will be caught immediately.
/// </summary>
public sealed class AccountFkIsolationShould
{
    [Fact]
    public void NotExposeEmailPropertyOnAnyNonAccountEntityType()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AStar.Dev.OneDriveSync.Infrastructure.Persistence.AppDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        using var context = new AStar.Dev.OneDriveSync.Infrastructure.Persistence.AppDbContext(options);

        // Act
        var violatingTypes = context.Model
            .GetEntityTypes()
            .Where(e => e.ClrType != typeof(Account))
            .Where(e => e.GetProperties().Any(p => p.Name.Equals("Email", StringComparison.OrdinalIgnoreCase)))
            .Select(e => e.ClrType.Name)
            .ToList();

        // Assert
        violatingTypes.ShouldBeEmpty(
            $"PII column 'Email' found on non-Account entity type(s): {string.Join(", ", violatingTypes)}. " +
            "All email data must live exclusively on the Account entity.");
    }

    [Fact]
    public void NotExposeDisplayNamePropertyOnAnyNonAccountEntityType()
    {
        var options = new DbContextOptionsBuilder<AStar.Dev.OneDriveSync.Infrastructure.Persistence.AppDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        using var context = new AStar.Dev.OneDriveSync.Infrastructure.Persistence.AppDbContext(options);

        var violatingTypes = context.Model
            .GetEntityTypes()
            .Where(e => e.ClrType != typeof(Account))
            .Where(e => e.GetProperties().Any(p => p.Name.Equals("DisplayName", StringComparison.OrdinalIgnoreCase)))
            .Select(e => e.ClrType.Name)
            .ToList();

        violatingTypes.ShouldBeEmpty(
            $"PII column 'DisplayName' found on non-Account entity type(s): {string.Join(", ", violatingTypes)}. " +
            "Display name data must live exclusively on the Account entity.");
    }

    [Fact]
    public void NotExposeMicrosoftAccountIdPropertyOnAnyNonAccountEntityType()
    {
        var options = new DbContextOptionsBuilder<AStar.Dev.OneDriveSync.Infrastructure.Persistence.AppDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        using var context = new AStar.Dev.OneDriveSync.Infrastructure.Persistence.AppDbContext(options);

        var violatingTypes = context.Model
            .GetEntityTypes()
            .Where(e => e.ClrType != typeof(Account))
            .Where(e => e.GetProperties().Any(p => p.Name.Equals("MicrosoftAccountId", StringComparison.OrdinalIgnoreCase)))
            .Select(e => e.ClrType.Name)
            .ToList();

        violatingTypes.ShouldBeEmpty(
            $"PII column 'MicrosoftAccountId' found on non-Account entity type(s): {string.Join(", ", violatingTypes)}. " +
            "Microsoft account IDs must live exclusively on the Account entity.");
    }

    [Fact]
    public async Task RejectInsertionOfRowWithInvalidAccountGuid()
    {
        // Arrange — verify that a FK violation (non-existent account_id) is rejected
        // at the SQLite layer when FK enforcement is active.
        await using var factory = AppDbContextFactory.Create();
        await using var context = await factory.CreateContextAsync();

        var accountTableName = context.Model
            .FindEntityType(typeof(Account))!
            .GetTableName();

        await context.Database.ExecuteSqlRawAsync($"""
            CREATE TABLE IF NOT EXISTS test_fk_violation (
                id          TEXT PRIMARY KEY NOT NULL,
                account_id  TEXT NOT NULL
                    REFERENCES {accountTableName} (id)
            )
            """);

        // Act — attempt to insert with a non-existent account_id (FK violation)
        var bogusAccountId = Guid.NewGuid();
        Func<Task> act = async () =>
            await context.Database.ExecuteSqlRawAsync(
                $"INSERT INTO test_fk_violation (id, account_id) VALUES ('{Guid.NewGuid()}', '{bogusAccountId}')");

        // Assert — SQLite should reject the insert with a FK constraint failure
        await act.ShouldThrowAsync<Microsoft.Data.Sqlite.SqliteException>();
    }
}
