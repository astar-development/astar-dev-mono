using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Accounts.TestsUnit;

public sealed class GivenASerialCloudSyncDatabase
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_twenty_callers_migrate_at_once_then_only_one_migration_runs_at_a_time_and_every_caller_runs()
    {
        var inner = new OverlapRecordingDatabase();
        using var database = new SerialCloudSyncDatabase(inner);

        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => database.MigrateAsync(CancellationToken), CancellationToken)));

        inner.ShouldSatisfyAllConditions(
            recorded => recorded.MostAtOnce.ShouldBe(1),
            recorded => recorded.Calls.ShouldBe(20));
        results.ShouldAllBe(result => result.Match(_ => true, _ => false));
    }

    [Fact]
    public async Task when_a_migration_throws_then_the_next_caller_still_migrates()
    {
        var inner = new OverlapRecordingDatabase { ThrowOnFirstCall = true };
        using var database = new SerialCloudSyncDatabase(inner);

        await Should.ThrowAsync<InvalidOperationException>(() => database.MigrateAsync(CancellationToken));
        var second = await database.MigrateAsync(CancellationToken);

        second.Match(_ => true, _ => false).ShouldBeTrue();
        inner.Calls.ShouldBe(2);
    }

    [Fact]
    public async Task when_the_migration_fails_then_the_failure_is_returned_unchanged()
    {
        var failure = new ErrorResponse("The CloudSync database could not be created or migrated.");
        var inner = Substitute.For<ICloudSyncDatabase>();
        inner.MigrateAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure<Unit, ErrorResponse>(failure));
        using var database = new SerialCloudSyncDatabase(inner);

        var result = await database.MigrateAsync(CancellationToken);

        result.Match(ok => (object)ok, error => error).ShouldBe(failure);
    }

    [Fact]
    public async Task when_the_caller_is_cancelled_while_waiting_then_its_migration_does_not_run()
    {
        var inner = new OverlapRecordingDatabase();
        using var database = new SerialCloudSyncDatabase(inner);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => database.MigrateAsync(cancelled.Token));

        inner.Calls.ShouldBe(0);
    }
}
