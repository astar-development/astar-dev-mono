using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Accounts.Sqlite.TestsIntegration;

public class GivenASqliteAccountStore
{
    private const string HomeAccountIdentifier = "00000000-0000-0000-a1b2-c3d4e5f60718.9188040d-6c67-4c5b-b112-36a304b66dad";
    private const string OtherHomeAccountIdentifier = "00000000-0000-0000-f1e2-d3c4b5a69788.9188040d-6c67-4c5b-b112-36a304b66dad";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_a_new_account_is_added_then_the_outcome_is_added()
    {
        await using var database = await TemporaryCloudSyncDatabase.CreateMigratedAsync(CancellationToken);

        var outcome = await database.AccountStore.AddAsync(Registration(HomeAccountIdentifier, "Personal"), CancellationToken);

        outcome.ShouldBeOfType<AccountAddOutcome.Added>();
    }

    [Fact]
    public async Task when_a_new_account_is_added_then_it_can_be_read_back_by_its_key()
    {
        await using var database = await TemporaryCloudSyncDatabase.CreateMigratedAsync(CancellationToken);
        var registration = Registration(HomeAccountIdentifier, "Personal");
        var added = (AccountAddOutcome.Added)await database.AccountStore.AddAsync(registration, CancellationToken);

        var found = await database.AccountStore.GetByKeyAsync(CloudProviderFactory.CreateOneDrive(), registration.Key, CancellationToken);

        found.Match(account => account, _ => Option.None<StoredAccount>()).ShouldBe(Option.Some(StoredAccountFactory.Create(added.AccountId, registration)));
    }

    [Fact]
    public async Task when_no_account_has_the_key_then_none_is_found()
    {
        await using var database = await TemporaryCloudSyncDatabase.CreateMigratedAsync(CancellationToken);

        var found = await database.AccountStore.GetByKeyAsync(CloudProviderFactory.CreateOneDrive(), Key(HomeAccountIdentifier), CancellationToken);

        found.Match(account => account.Match(_ => "found", () => "none"), _ => "failed").ShouldBe("none");
    }

    [Fact]
    public async Task when_the_same_account_is_added_again_then_it_is_reauthenticated_with_its_original_id()
    {
        await using var database = await TemporaryCloudSyncDatabase.CreateMigratedAsync(CancellationToken);
        var added = (AccountAddOutcome.Added)await database.AccountStore.AddAsync(Registration(HomeAccountIdentifier, "Personal"), CancellationToken);

        var outcome = await database.AccountStore.AddAsync(Registration(HomeAccountIdentifier, "Personal"), CancellationToken);

        outcome.ShouldBe(AccountAddOutcomeFactory.CreateReAuthenticated(added.AccountId));
    }

    [Fact]
    public async Task when_the_same_account_is_added_again_with_different_casing_then_it_is_the_same_account()
    {
        await using var database = await TemporaryCloudSyncDatabase.CreateMigratedAsync(CancellationToken);
        var added = (AccountAddOutcome.Added)await database.AccountStore.AddAsync(Registration(HomeAccountIdentifier, "Personal"), CancellationToken);

        var outcome = await database.AccountStore.AddAsync(Registration(HomeAccountIdentifier.ToUpperInvariant(), "Personal"), CancellationToken);

        outcome.ShouldBe(AccountAddOutcomeFactory.CreateReAuthenticated(added.AccountId));
    }

    [Fact]
    public async Task when_the_same_account_is_added_again_then_its_profile_and_key_casing_come_from_the_latest_sign_in()
    {
        await using var database = await TemporaryCloudSyncDatabase.CreateMigratedAsync(CancellationToken);
        var added = (AccountAddOutcome.Added)await database.AccountStore.AddAsync(Registration(HomeAccountIdentifier, "Personal"), CancellationToken);
        var refreshed = AccountRegistrationFactory.Create(Key(HomeAccountIdentifier.ToUpperInvariant()), CloudProviderFactory.CreateOneDrive(), "Renamed", "renamed@example.com", new DriveId("NEWDRIVE01"), StorageUsageFactory.Create(2_048, 1_099_511_627_776));
        await database.AccountStore.AddAsync(refreshed, CancellationToken);

        var all = await database.AccountStore.GetAllAsync(CancellationToken);

        all.Match(accounts => accounts, _ => []).ShouldBe([StoredAccountFactory.Create(added.AccountId, refreshed)]);
    }

    [Fact]
    public async Task when_two_different_accounts_are_added_then_both_are_listed()
    {
        await using var database = await TemporaryCloudSyncDatabase.CreateMigratedAsync(CancellationToken);
        await database.AccountStore.AddAsync(Registration(HomeAccountIdentifier, "Personal"), CancellationToken);
        await database.AccountStore.AddAsync(Registration(OtherHomeAccountIdentifier, "Photos"), CancellationToken);

        var all = await database.AccountStore.GetAllAsync(CancellationToken);

        all.Match(accounts => accounts.Select(account => account.Registration.DisplayName).Order().ToList(), _ => []).ShouldBe(["Personal", "Photos"]);
    }

    [Fact]
    public async Task when_the_same_account_is_added_many_times_at_once_then_it_is_added_once_and_stored_once()
    {
        await using var database = await TemporaryCloudSyncDatabase.CreateMigratedAsync(CancellationToken);
        var identifierCasings = Enumerable.Range(0, 20).Select(attempt => attempt % 2 == 0 ? HomeAccountIdentifier : HomeAccountIdentifier.ToUpperInvariant());

        var outcomes = await Task.WhenAll(identifierCasings.Select(identifier => Task.Run(() => database.AccountStore.AddAsync(Registration(identifier, "Personal"), CancellationToken), CancellationToken)));
        var all = await database.AccountStore.GetAllAsync(CancellationToken);

        (outcomes.Count(outcome => outcome is AccountAddOutcome.Added), outcomes.Count(outcome => outcome is AccountAddOutcome.ReAuthenticated)).ShouldBe((1, 19));
        all.Match(accounts => accounts.Count, _ => -1).ShouldBe(1);
    }

    [Fact]
    public async Task when_the_account_cannot_be_saved_then_the_outcome_is_persist_failed()
    {
        await using var database = TemporaryCloudSyncDatabase.CreateUnmigrated();

        var outcome = await database.AccountStore.AddAsync(Registration(HomeAccountIdentifier, "Personal"), CancellationToken);

        outcome.ShouldBe(AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreatePersistFailed()));
    }

    [Fact]
    public async Task when_the_account_cannot_be_saved_then_an_error_is_logged()
    {
        await using var database = TemporaryCloudSyncDatabase.CreateUnmigrated();

        await database.AccountStore.AddAsync(Registration(HomeAccountIdentifier, "Personal"), CancellationToken);

        database.Logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Error && entry.Exception == null);
    }

    [Fact]
    public async Task when_the_accounts_cannot_be_read_then_the_list_fails()
    {
        await using var database = TemporaryCloudSyncDatabase.CreateUnmigrated();

        var all = await database.AccountStore.GetAllAsync(CancellationToken);

        all.Match(_ => true, _ => false).ShouldBeFalse();
    }

    [Fact]
    public async Task when_an_account_cannot_be_looked_up_then_the_lookup_fails()
    {
        await using var database = TemporaryCloudSyncDatabase.CreateUnmigrated();

        var found = await database.AccountStore.GetByKeyAsync(CloudProviderFactory.CreateOneDrive(), Key(HomeAccountIdentifier), CancellationToken);

        found.Match(_ => true, _ => false).ShouldBeFalse();
    }

    private static ProviderAccountKey Key(string identifier) => ProviderAccountKeyFactory.Create(identifier).Match(key => key, _ => throw new InvalidOperationException("The test key must be valid."));

    private static AccountRegistration Registration(string identifier, string displayName) => AccountRegistrationFactory.Create(Key(identifier), CloudProviderFactory.CreateOneDrive(), displayName, $"{displayName.ToLowerInvariant()}@example.com", new DriveId("8A3F2C1D0E9B7A65"), StorageUsageFactory.Create(1_024, 5_368_709_120));
}
