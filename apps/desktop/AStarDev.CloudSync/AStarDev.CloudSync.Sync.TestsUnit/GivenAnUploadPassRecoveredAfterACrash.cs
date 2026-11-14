using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenAnUploadPassRecoveredAfterACrash : IDisposable
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

    private ResolutionFixture? fixture;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public void Dispose() => fixture?.Dispose();

    [Fact]
    public async Task when_a_pass_crashes_after_one_upload_finished_then_the_recovery_pass_does_not_upload_it_again()
    {
        var run = await ResolutionFixture.CreateAsync(CancellationToken, [SelectionTestRules.Include("Docs")]);
        fixture = run;
        run.Remote.Folder("Docs", "docs-id");
        _ = run.Local("Docs/a.txt", "first");
        _ = run.Local("Docs/b.txt", "second");
        using var crash = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
        var defaultResponse = run.Uploader.Respond;
        run.Uploader.Respond = name =>
        {
            if (name != "b.txt")
                return defaultResponse(name);

            _ = SpinWait.SpinUntil(() => run.Recorder.Recorded.Count >= 1, Limit);
            crash.Cancel();

            return Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateTransient());
        };
        var firstChanges = await Changes(run);
        _ = await Record.ExceptionAsync(() => run.Uploads.RunAsync(run.Account, firstChanges, crash.Token));
        var uploadedBeforeTheCrash = run.Uploader.Calls.Count(call => call.Name == "a.txt");
        run.Uploader.Respond = defaultResponse;

        var recoveryChanges = await Changes(run);
        _ = await run.Uploads.RunAsync(run.Account, recoveryChanges, CancellationToken);

        (uploadedBeforeTheCrash, run.Uploader.Calls.Count(call => call.Name == "a.txt"), run.Uploader.Calls.Count(call => call.Name == "b.txt"), run.Baseline.Has("Docs/a.txt"), run.Baseline.Has("Docs/b.txt")).ShouldBe((1, 1, 2, true, true));
    }

    private static async Task<LocalChangeSet> Changes(ResolutionFixture run) =>
        (await run.Detector.DetectAsync(run.Account, CancellationToken)).Match(changes => changes, failure => throw new InvalidOperationException($"The scan failed: {failure.GetType().Name}"));
}
