using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Integration.FileClassification;

public sealed class GivenFileClassificationRuleRepository(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>, IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        var factory = fixture.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var context = await factory.CreateDbContextAsync();
        context.FileClassificationRules.RemoveRange(context.FileClassificationRules);
        await context.SaveChangesAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task when_adding_a_rule_then_it_can_be_retrieved()
    {
        var ct = TestContext.Current.CancellationToken;
        var repository = fixture.Services.GetRequiredService<IFileClassificationRuleRepository>();

        await repository.AddAsync(BuildRule(["invoice", "receipt"], "Finance"), ct);

        var rules = await repository.GetAllAsync(ct);
        rules.Count.ShouldBe(1);
        rules[0].Classification.Level1.ShouldBe("Finance");
    }

    [Fact]
    public async Task when_adding_multiple_rules_then_all_are_returned()
    {
        var ct = TestContext.Current.CancellationToken;
        var repository = fixture.Services.GetRequiredService<IFileClassificationRuleRepository>();

        await repository.AddAsync(BuildRule(["invoice"], "Finance"), ct);
        await repository.AddAsync(BuildRule(["photo", "image"], "Photos"), ct);
        await repository.AddAsync(BuildRule(["contract", "legal"], "Documents"), ct);

        var rules = await repository.GetAllAsync(ct);
        rules.Count.ShouldBe(3);
    }

    [Fact]
    public async Task when_no_rules_exist_then_an_empty_collection_is_returned()
    {
        var ct = TestContext.Current.CancellationToken;
        var repository = fixture.Services.GetRequiredService<IFileClassificationRuleRepository>();

        var rules = await repository.GetAllAsync(ct);

        rules.ShouldBeEmpty();
    }

    private static FileClassificationRule BuildRule(IReadOnlyList<string> keywords, string level1) => FileClassificationRuleFactory.Create(
        keywords,
        FileClassificationFactory.Create(level1, Option.None<string>(), Option.None<string>(), isSpecial: false));
}
