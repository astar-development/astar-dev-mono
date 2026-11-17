using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class GivenASyncRootStoreWithChangeParticipants : IAsyncLifetime
{
    private const string OneDrive = "/home/jbarden/CloudSync/OneDrive";
    private const string Elsewhere = "/home/jbarden/CloudSync/Elsewhere";
    private static readonly AccountId FirstAccount = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly AccountId SecondAccount = AccountIdFactory.Create(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    private readonly RecordingRootChangeParticipant recording = new();
    private TemporarySyncDatabase? database;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private TemporarySyncDatabase Database => database ?? throw new InvalidOperationException("The database is created before each test.");

    public async ValueTask InitializeAsync() =>
        database = await TemporarySyncDatabase.CreateMigratedAsync(CancellationToken, services => services
            .AddSingleton<ISyncRootChangeParticipant, ThrowingRootChangeParticipant>()
            .AddSingleton<ISyncRootChangeParticipant>(recording));

    public async ValueTask DisposeAsync()
    {
        if (database is not null)
            await database.DisposeAsync();
    }

    private async Task<string> SetRootAsync(AccountId account, string path) =>
        (await Database.Writer.SetAsync(account, Database.Folder(path), CancellationToken)).Match(root => root.Directory.FullName, rejection => rejection.GetType().Name);

    [Fact]
    public async Task when_the_root_changes_then_every_participant_after_a_throwing_one_is_called_once_for_that_account_and_the_save_succeeds()
    {
        _ = await SetRootAsync(FirstAccount, OneDrive);

        var saved = await SetRootAsync(FirstAccount, Elsewhere);

        saved.ShouldBe(Elsewhere);
        recording.Calls.ShouldBe([FirstAccount]);
    }

    [Fact]
    public async Task when_a_root_is_set_for_the_first_time_then_no_participant_is_called()
    {
        _ = await SetRootAsync(FirstAccount, OneDrive);

        recording.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_same_root_is_saved_again_then_no_participant_is_called()
    {
        _ = await SetRootAsync(FirstAccount, OneDrive);

        _ = await SetRootAsync(FirstAccount, OneDrive + "/");

        recording.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_root_change_is_rejected_then_no_participant_is_called()
    {
        _ = await SetRootAsync(FirstAccount, OneDrive);
        _ = await SetRootAsync(SecondAccount, Elsewhere);

        var saved = await SetRootAsync(SecondAccount, OneDrive + "/Work");

        (saved == Elsewhere, recording.Calls.Count).ShouldBe((false, 0));
    }
}
