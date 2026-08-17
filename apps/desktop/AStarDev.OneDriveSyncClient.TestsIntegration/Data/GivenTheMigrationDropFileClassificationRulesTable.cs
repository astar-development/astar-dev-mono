using AStar.Dev.Infrastructure.AppDb;
using AStarDev.OneDriveSyncClient.TestsIntegration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.OneDriveSyncClient.TestsIntegration.Data;

[Collection(IntegrationTestGrouping.Name)]
public sealed class GivenTheMigrationDropFileClassificationRulesTable(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task when_all_migrations_are_applied_then_file_classification_rules_table_does_not_exist()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var context = fixture.Services.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();

        int tableCount = await context.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS Value FROM sqlite_master WHERE type='table' AND name='FileClassificationRules'"
        ).FirstOrDefaultAsync(ct);

        tableCount.ShouldBe(0);
    }
}
