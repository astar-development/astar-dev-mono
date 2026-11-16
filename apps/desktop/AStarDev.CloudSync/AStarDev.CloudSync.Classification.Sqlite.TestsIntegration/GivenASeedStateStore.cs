using static AStarDev.CloudSync.Classification.Sqlite.TestsIntegration.ClassificationData;

namespace AStarDev.CloudSync.Classification.Sqlite.TestsIntegration;

public sealed class GivenASeedStateStore : MigratedDatabaseTests
{
    private async Task<string?> VersionAsync() => Ok(await Database.SeedState.GetAppliedVersionAsync(CancellationToken)).Match(version => (string?)version, () => null);

    [Fact]
    public async Task when_no_seed_was_applied_then_no_version_is_read()
    {
        (await VersionAsync()).ShouldBeNull();
    }

    [Fact]
    public async Task when_a_version_is_set_then_it_is_read_back()
    {
        _ = Ok(await Database.SeedState.SetAppliedVersionAsync("v1", CancellationToken));

        (await VersionAsync()).ShouldBe("v1");
    }

    [Fact]
    public async Task when_a_version_is_set_again_then_it_replaces_the_first()
    {
        _ = Ok(await Database.SeedState.SetAppliedVersionAsync("v1", CancellationToken));

        _ = Ok(await Database.SeedState.SetAppliedVersionAsync("v2", CancellationToken));

        (await VersionAsync()).ShouldBe("v2");
    }

    [Fact]
    public async Task when_the_version_is_set_by_twenty_parallel_calls_then_one_of_the_values_is_stored()
    {
        _ = await InParallelAsync(20, async index => Ok(await Database.SeedState.SetAppliedVersionAsync($"v{index}", CancellationToken)));

        (await VersionAsync()).ShouldStartWith("v");
    }

    [Fact]
    public async Task when_the_service_restarts_then_the_version_is_still_there()
    {
        _ = Ok(await Database.SeedState.SetAppliedVersionAsync("v1", CancellationToken));

        await Database.RestartAsync();

        (await VersionAsync()).ShouldBe("v1");
    }
}
