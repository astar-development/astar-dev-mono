using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AStar.Dev.OneDriveSync.Features.Accounts;
using AStar.Dev.OneDriveSync.Tests.Integration.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace AStar.Dev.OneDriveSync.Tests.Integration.Persistence;

public sealed class GivenTheDatabaseSchema
{
    [Theory]
    [InlineData("Email")]
    [InlineData("DisplayName")]
    [InlineData("MicrosoftAccountId")]
    public void when_inspected_then_pii_column_is_confined_to_the_account_entity_type(string piiColumnName)
    {
        using var context = AppDbContextFactory.CreateForModelInspection();

        var violatingTypes = context.Model
            .GetEntityTypes()
            .Where(e => e.ClrType != typeof(Account))
            .Where(e => e.GetProperties().Any(p => p.Name.Equals(piiColumnName, StringComparison.OrdinalIgnoreCase)))
            .Select(e => e.ClrType.Name)
            .ToList();

        violatingTypes.ShouldBeEmpty($"PII column '{piiColumnName}' must live exclusively on the Account entity.");
    }

    [Fact]
    public async Task when_a_row_references_a_non_existent_account_then_the_database_rejects_the_insert()
    {
        await using var factory = AppDbContextFactory.Create();
        await using var context = await factory.CreateContextAsync(TestContext.Current.CancellationToken);
        await context.CreateStubChildTableAsync("test_fk_violation", cascadeOnDelete: false, cancellationToken: TestContext.Current.CancellationToken);

        Func<Task> act = async () =>
            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO test_fk_violation (id, account_id) VALUES ({0}, {1})",
                (IEnumerable<object>)[Guid.NewGuid().ToString(), Guid.NewGuid().ToString()],
                TestContext.Current.CancellationToken);

        _ = await act.ShouldThrowAsync<SqliteException>();
    }
}
