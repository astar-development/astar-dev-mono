using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenAVerboseConflictResolution
{
    private static readonly IReadOnlyList<string> PersonalValues = PersonalData.All;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_conflicts_are_resolved_and_refused_over_personal_names_then_nothing_personal_is_in_any_log_entry()
    {
        using var run = await ResolutionFixture.CreateAsync(CancellationToken);
        const string Mine = $"Docs/SecretClientFolder/{PersonalData.DisplayName} passport-scan {PersonalData.AccessToken} mine.txt";
        const string Theirs = $"Docs/SecretClientFolder/{PersonalData.DisplayName} passport-scan {PersonalData.RefreshToken} theirs.txt";
        const string Both = $"Docs/SecretClientFolder/{PersonalData.Email} {PersonalData.BearerToken} both.txt";
        const string Stale = "Docs/SecretClientFolder/passport-scan stale.txt";
        run.Local(Mine, "mine");
        run.Remote.File(Mine, PersonalData.DownloadUrl, "c-seen", 4);
        run.Local(Theirs, "mine");
        run.Remote.File(Theirs, "id-theirs", "c-seen", 6);
        run.Downloader.Content["id-theirs"] = "theirs"u8.ToArray();
        run.Local(Both, "mine");
        run.Remote.File(Both, "id-both", "c-seen", 6);
        run.Downloader.Content["id-both"] = "theirs"u8.ToArray();
        run.Local(Stale, "mine");
        run.Remote.File(Stale, "id-stale", "c-newer", 4);
        var mine = run.Conflict(Mine, ConflictKindFactory.CreateBothModified(), seenCTag: "c-seen");
        var theirs = run.Conflict(Theirs, ConflictKindFactory.CreateBothModified(), seenCTag: "c-seen");
        var both = run.Conflict(Both, ConflictKindFactory.CreateBothModified(), seenCTag: "c-seen");
        var stale = run.Conflict(Stale, ConflictKindFactory.CreateBothModified(), seenCTag: "c-seen");

        _ = await run.Resolver.ResolveAsync(run.Account, [mine], ConflictStrategyFactory.CreateLocalWins(), CancellationToken);
        _ = await run.Resolver.ResolveAsync(run.Account, [theirs], ConflictStrategyFactory.CreateRemoteWins(), CancellationToken);
        _ = await run.Resolver.ResolveAsync(run.Account, [both], ConflictStrategyFactory.CreateKeepBoth(), CancellationToken);
        _ = await run.Resolver.ResolveAsync(run.Account, [stale], ConflictStrategyFactory.CreateLocalWins(), CancellationToken);

        var logged = LoggedText.From(run.Logs).ToList();
        logged.ShouldNotBeEmpty();
        LoggedText.Personal(logged, PersonalValues).ShouldBeEmpty();
    }
}
