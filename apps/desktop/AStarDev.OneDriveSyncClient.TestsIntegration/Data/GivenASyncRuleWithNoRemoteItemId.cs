using AStar.Dev.Infrastructure.AppDb;
using AStarDev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.TestsIntegration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.OneDriveSyncClient.TestsIntegration.Data;

[Collection(IntegrationTestGrouping.Name)]
public sealed class GivenASyncRuleWithNoRemoteItemId(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task when_the_rule_is_read_back_then_remote_item_id_is_none()
    {
        var ct = TestContext.Current.CancellationToken;
        var accountId = new AccountId("sync-rule-no-remote-item-id");
        var factory = fixture.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using (var seedContext = await factory.CreateDbContextAsync(ct))
        {
            seedContext.Set<AccountEntity>().Add(new AccountEntity { Id = accountId });
            await seedContext.SaveChangesAsync(ct);
        }

        var repository = fixture.Services.GetRequiredService<ISyncRuleRepository>();
        await repository.UpsertAsync(accountId, "/LocalOnlyFolder", RuleType.Include, null, ct);

        await using var readContext = await factory.CreateDbContextAsync(ct);
        var rule = await readContext.SyncRules.SingleAsync(r => r.AccountId == accountId, ct);

        rule.RemoteItemId.ShouldBe(Option.None<string>());
    }
}
