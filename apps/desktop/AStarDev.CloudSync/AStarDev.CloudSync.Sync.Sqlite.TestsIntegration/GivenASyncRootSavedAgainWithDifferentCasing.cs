using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class GivenASyncRootSavedAgainWithDifferentCasing
{
    private const string OneDrive = "/home/jbarden/CloudSync/OneDrive";
    private const string OneDriveLowerCase = "/home/jbarden/CloudSync/onedrive";
    private const string Elsewhere = "/home/jbarden/CloudSync/Elsewhere";
    private static readonly AccountId FirstAccount = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    private readonly RecordingRootChangeParticipant recording = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_the_file_system_ignores_case_then_the_baseline_is_kept_no_participant_is_called_and_the_new_casing_is_saved()
    {
        await using var database = await CreateDatabaseAsync(StringComparison.OrdinalIgnoreCase);
        _ = await SetRootAsync(database, OneDrive);
        await SeedBaselineAsync(database, 3);

        var saved = await SetRootAsync(database, OneDriveLowerCase);

        (saved, await StoredRootAsync(database), await BaselineCountAsync(database), recording.Calls.Count).ShouldBe((OneDriveLowerCase, OneDriveLowerCase, 3, 0));
    }

    [Fact]
    public async Task when_the_file_system_is_case_sensitive_then_the_baseline_is_cleared_and_every_participant_is_called()
    {
        await using var database = await CreateDatabaseAsync(StringComparison.Ordinal);
        _ = await SetRootAsync(database, OneDrive);
        await SeedBaselineAsync(database, 3);

        var saved = await SetRootAsync(database, OneDriveLowerCase);

        (saved, await BaselineCountAsync(database)).ShouldBe((OneDriveLowerCase, 0));
        recording.Calls.ShouldBe([FirstAccount]);
    }

    [Fact]
    public async Task when_the_file_system_ignores_case_and_the_root_really_changes_then_the_baseline_is_cleared_and_every_participant_is_called()
    {
        await using var database = await CreateDatabaseAsync(StringComparison.OrdinalIgnoreCase);
        _ = await SetRootAsync(database, OneDrive);
        await SeedBaselineAsync(database, 3);

        var saved = await SetRootAsync(database, Elsewhere);

        (saved, await BaselineCountAsync(database)).ShouldBe((Elsewhere, 0));
        recording.Calls.ShouldBe([FirstAccount]);
    }

    private Task<TemporarySyncDatabase> CreateDatabaseAsync(StringComparison comparison) =>
        TemporarySyncDatabase.CreateMigratedAsync(CancellationToken, services => services
            .AddSingleton<IFileSystemCaseRule>(new FixedFileSystemCaseRule(comparison))
            .AddSingleton<ISyncRootChangeParticipant>(recording));

    private static async Task<string> SetRootAsync(TemporarySyncDatabase database, string path) =>
        (await database.Writer.SetAsync(FirstAccount, database.Folder(path), CancellationToken)).Match(root => root.Directory.FullName, rejection => rejection.GetType().Name);

    private static async Task<string> StoredRootAsync(TemporarySyncDatabase database) =>
        (await database.Reader.GetAsync(FirstAccount, CancellationToken)).Match(root => root.Match(found => found.Directory.FullName, () => "none"), error => error.Message);

    private static async Task SeedBaselineAsync(TemporarySyncDatabase database, int count)
    {
        var entries = Enumerable.Range(0, count).Select(index => SyncBaselineEntryFactory.Create(FirstAccount, RelativePath($"Docs{index}"), RemoteItemIdFactory.Create($"item-{index}").Match(id => id, _ => throw new InvalidOperationException("The test id must be valid.")), SyncBaselineKindFactory.CreateFolder())).ToList();
        _ = await database.BaselineWriter.UpsertAsync(FirstAccount, entries, CancellationToken);
    }

    private static async Task<int> BaselineCountAsync(TemporarySyncDatabase database) =>
        (await database.BaselineReader.GetUnderAsync(FirstAccount, default, Option.None<RelativeSyncPath>(), 100, CancellationToken)).Match(page => page.Count, _ => -1);

    private static RelativeSyncPath RelativePath(string value) => RelativeSyncPathFactory.Create(value).Match(path => path, _ => throw new InvalidOperationException("The test path must be valid."));
}
