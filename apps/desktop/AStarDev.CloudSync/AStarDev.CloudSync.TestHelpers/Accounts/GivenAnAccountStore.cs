using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Shouldly;
using Xunit;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public abstract class GivenAnAccountStore : IAsyncLifetime
{
    private const string HomeAccountIdentifier = "00000000-0000-0000-a1b2-c3d4e5f60718.9188040d-6c67-4c5b-b112-36a304b66dad";
    private const string OtherHomeAccountIdentifier = "00000000-0000-0000-f1e2-d3c4b5a69788.9188040d-6c67-4c5b-b112-36a304b66dad";
    private const string ThirdHomeAccountIdentifier = "00000000-0000-0000-0a1b-2c3d4e5f6071.9188040d-6c67-4c5b-b112-36a304b66dad";
    private const string PersonalDisplayName = "Personal";

    private IAccountStore? store;

    protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    protected IAccountStore Store => store ?? throw new InvalidOperationException("The store is created before each test.");

    public async ValueTask InitializeAsync() => store = await CreateStoreAsync(CancellationToken);

    public virtual ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        return ValueTask.CompletedTask;
    }

    protected abstract Task<IAccountStore> CreateStoreAsync(CancellationToken cancellationToken);

    [Fact]
    public async Task when_a_new_account_is_added_then_the_outcome_is_added()
    {
        var outcome = await Store.AddAsync(Registration(HomeAccountIdentifier, PersonalDisplayName), CancellationToken);

        outcome.ShouldBeOfType<AccountAddOutcome.Added>();
    }

    [Fact]
    public async Task when_a_new_account_is_added_then_it_can_be_read_back_by_its_key()
    {
        var registration = Registration(HomeAccountIdentifier, PersonalDisplayName);
        var added = (await Store.AddAsync(registration, CancellationToken)).ShouldBeOfType<AccountAddOutcome.Added>();

        var found = await Store.GetByKeyAsync(CloudProviderFactory.CreateOneDrive(), registration.Key, CancellationToken);

        found.Match(account => account, _ => Option.None<StoredAccount>()).ShouldBe(Option.Some(StoredAccountFactory.Create(added.AccountId, registration)));
    }

    [Fact]
    public async Task when_an_account_is_looked_up_with_a_casing_variant_of_its_key_then_it_is_found()
    {
        var registration = Registration(HomeAccountIdentifier, PersonalDisplayName);
        var added = (await Store.AddAsync(registration, CancellationToken)).ShouldBeOfType<AccountAddOutcome.Added>();

        var found = await Store.GetByKeyAsync(CloudProviderFactory.CreateOneDrive(), Key(HomeAccountIdentifier.ToUpperInvariant()), CancellationToken);

        found.Match(account => account, _ => Option.None<StoredAccount>()).ShouldBe(Option.Some(StoredAccountFactory.Create(added.AccountId, registration)));
    }

    [Fact]
    public async Task when_no_account_has_the_key_then_none_is_found()
    {
        await Store.AddAsync(Registration(OtherHomeAccountIdentifier, PersonalDisplayName), CancellationToken);

        var found = await Store.GetByKeyAsync(CloudProviderFactory.CreateOneDrive(), Key(HomeAccountIdentifier), CancellationToken);

        found.Match(account => account.Match(_ => "found", () => "none"), _ => "failed").ShouldBe("none");
    }

    [Fact]
    public async Task when_the_same_account_is_added_again_then_it_is_reauthenticated_with_its_original_id()
    {
        var added = (await Store.AddAsync(Registration(HomeAccountIdentifier, PersonalDisplayName), CancellationToken)).ShouldBeOfType<AccountAddOutcome.Added>();

        var outcome = await Store.AddAsync(Registration(HomeAccountIdentifier, PersonalDisplayName), CancellationToken);

        outcome.ShouldBe(AccountAddOutcomeFactory.CreateReAuthenticated(added.AccountId));
    }

    [Fact]
    public async Task when_the_same_account_is_added_again_with_different_casing_then_it_is_the_same_account()
    {
        var added = (await Store.AddAsync(Registration(HomeAccountIdentifier, PersonalDisplayName), CancellationToken)).ShouldBeOfType<AccountAddOutcome.Added>();

        var outcome = await Store.AddAsync(Registration(HomeAccountIdentifier.ToUpperInvariant(), PersonalDisplayName), CancellationToken);

        outcome.ShouldBe(AccountAddOutcomeFactory.CreateReAuthenticated(added.AccountId));
    }

    [Fact]
    public async Task when_the_same_account_is_added_again_then_its_profile_storage_and_key_casing_come_from_the_latest_sign_in()
    {
        var added = (await Store.AddAsync(Registration(HomeAccountIdentifier, PersonalDisplayName), CancellationToken)).ShouldBeOfType<AccountAddOutcome.Added>();
        var refreshed = AccountRegistrationFactory.Create(Key(HomeAccountIdentifier.ToUpperInvariant()), CloudProviderFactory.CreateOneDrive(), "Renamed", "renamed@example.com", new DriveId("NEWDRIVE01"), StorageUsageFactory.Create(2_048, 1_099_511_627_776));
        await Store.AddAsync(refreshed, CancellationToken);

        var all = await Store.GetAllAsync(CancellationToken);

        all.Match(accounts => accounts, _ => []).ShouldBe([StoredAccountFactory.Create(added.AccountId, refreshed)]);
    }

    [Fact]
    public async Task when_no_account_has_been_added_then_none_are_listed()
    {
        var all = await Store.GetAllAsync(CancellationToken);

        all.Match(accounts => accounts.Count, _ => -1).ShouldBe(0);
    }

    [Fact]
    public async Task when_different_accounts_are_added_then_each_is_listed_once_in_any_order()
    {
        var personal = (await Store.AddAsync(Registration(HomeAccountIdentifier, PersonalDisplayName), CancellationToken)).ShouldBeOfType<AccountAddOutcome.Added>();
        var photos = (await Store.AddAsync(Registration(OtherHomeAccountIdentifier, "Photos"), CancellationToken)).ShouldBeOfType<AccountAddOutcome.Added>();
        var archive = (await Store.AddAsync(Registration(ThirdHomeAccountIdentifier, "Archive"), CancellationToken)).ShouldBeOfType<AccountAddOutcome.Added>();

        var all = await Store.GetAllAsync(CancellationToken);

        all.Match(accounts => accounts.Select(account => account.Id), _ => []).ShouldBe([personal.AccountId, photos.AccountId, archive.AccountId], ignoreOrder: true);
    }

    [Fact]
    public async Task when_the_same_account_is_added_many_times_at_once_then_it_is_added_once_and_stored_once()
    {
        var identifierCasings = Enumerable.Range(0, 20).Select(attempt => attempt % 2 == 0 ? HomeAccountIdentifier : HomeAccountIdentifier.ToUpperInvariant());

        var outcomes = await Task.WhenAll(identifierCasings.Select(identifier => Task.Run(() => Store.AddAsync(Registration(identifier, PersonalDisplayName), CancellationToken), CancellationToken)));
        var all = await Store.GetAllAsync(CancellationToken);

        (outcomes.Count(outcome => outcome is AccountAddOutcome.Added), outcomes.Count(outcome => outcome is AccountAddOutcome.ReAuthenticated)).ShouldBe((1, 19));
        all.Match(accounts => accounts.Count, _ => -1).ShouldBe(1);
    }

    [Fact]
    public async Task when_different_accounts_are_added_at_once_then_each_is_added_and_stored()
    {
        var identifiers = Enumerable.Range(0, 20).Select(attempt => $"{attempt:D8}-0000-0000-a1b2-c3d4e5f60718.9188040d-6c67-4c5b-b112-36a304b66dad").ToList();

        var outcomes = await Task.WhenAll(identifiers.Select(identifier => Task.Run(() => Store.AddAsync(Registration(identifier, PersonalDisplayName), CancellationToken), CancellationToken)));
        var all = await Store.GetAllAsync(CancellationToken);

        outcomes.Count(outcome => outcome is AccountAddOutcome.Added).ShouldBe(20);
        all.Match(accounts => accounts.Select(account => account.Registration.Key.Value), _ => []).ShouldBe(identifiers, ignoreOrder: true);
    }

    [Fact]
    public async Task when_an_account_is_removed_then_it_is_no_longer_found_or_listed()
    {
        var removed = (await Store.AddAsync(Registration(HomeAccountIdentifier, PersonalDisplayName), CancellationToken)).ShouldBeOfType<AccountAddOutcome.Added>().AccountId;
        var kept = (await Store.AddAsync(Registration(OtherHomeAccountIdentifier, "Photos"), CancellationToken)).ShouldBeOfType<AccountAddOutcome.Added>().AccountId;

        var result = await Store.RemoveAsync(removed, CancellationToken);

        result.Match(_ => "removed", error => error.Message).ShouldBe("removed");
        (await Store.GetAllAsync(CancellationToken)).Match(accounts => accounts.Select(account => account.Id), _ => []).ShouldBe([kept]);
        (await Store.GetByKeyAsync(CloudProviderFactory.CreateOneDrive(), Key(HomeAccountIdentifier), CancellationToken)).Match(found => found.Match(_ => "found", () => "none"), _ => "failed").ShouldBe("none");
    }

    [Fact]
    public async Task when_an_unknown_or_already_removed_account_is_removed_then_it_succeeds()
    {
        var added = (await Store.AddAsync(Registration(HomeAccountIdentifier, PersonalDisplayName), CancellationToken)).ShouldBeOfType<AccountAddOutcome.Added>().AccountId;
        _ = await Store.RemoveAsync(added, CancellationToken);

        var results = new[] { await Store.RemoveAsync(added, CancellationToken), await Store.RemoveAsync(AccountIdFactory.Create(Guid.NewGuid()), CancellationToken) };

        results.ShouldAllBe(result => result.Match(_ => true, _ => false));
    }

    [Fact]
    public async Task when_a_removed_account_signs_in_again_then_it_is_added_with_a_new_id()
    {
        var first = (await Store.AddAsync(Registration(HomeAccountIdentifier, PersonalDisplayName), CancellationToken)).ShouldBeOfType<AccountAddOutcome.Added>().AccountId;
        _ = await Store.RemoveAsync(first, CancellationToken);

        var again = await Store.AddAsync(Registration(HomeAccountIdentifier, PersonalDisplayName), CancellationToken);

        again.ShouldBeOfType<AccountAddOutcome.Added>().AccountId.ShouldNotBe(first);
    }

    [Fact]
    public async Task when_one_account_is_removed_many_times_at_once_then_every_removal_succeeds_and_it_is_gone()
    {
        var added = (await Store.AddAsync(Registration(HomeAccountIdentifier, PersonalDisplayName), CancellationToken)).ShouldBeOfType<AccountAddOutcome.Added>().AccountId;

        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => Store.RemoveAsync(added, CancellationToken), CancellationToken)));

        results.ShouldAllBe(result => result.Match(_ => true, _ => false));
        (await Store.GetAllAsync(CancellationToken)).Match(accounts => accounts.Count, _ => -1).ShouldBe(0);
    }

    [Fact]
    public async Task when_an_account_is_added_with_a_cancelled_token_then_it_is_cancelled_and_nothing_is_saved()
    {
        var add = () => Store.AddAsync(Registration(HomeAccountIdentifier, PersonalDisplayName), new CancellationToken(true));

        await Should.ThrowAsync<OperationCanceledException>(add);
        (await Store.GetAllAsync(CancellationToken)).Match(accounts => accounts.Count, _ => -1).ShouldBe(0);
    }

    [Fact]
    public async Task when_accounts_are_read_with_a_cancelled_token_then_the_reads_are_cancelled()
    {
        var readAll = () => Store.GetAllAsync(new CancellationToken(true));
        var readByKey = () => Store.GetByKeyAsync(CloudProviderFactory.CreateOneDrive(), Key(HomeAccountIdentifier), new CancellationToken(true));

        await Should.ThrowAsync<OperationCanceledException>(readAll);
        await Should.ThrowAsync<OperationCanceledException>(readByKey);
    }

    [Fact]
    public async Task when_an_account_is_removed_with_a_cancelled_token_then_it_is_cancelled_and_the_account_is_kept()
    {
        var added = (await Store.AddAsync(Registration(HomeAccountIdentifier, PersonalDisplayName), CancellationToken)).ShouldBeOfType<AccountAddOutcome.Added>().AccountId;

        var remove = () => Store.RemoveAsync(added, new CancellationToken(true));

        await Should.ThrowAsync<OperationCanceledException>(remove);
        (await Store.GetAllAsync(CancellationToken)).Match(accounts => accounts.Count, _ => -1).ShouldBe(1);
    }

    private static ProviderAccountKey Key(string identifier) => ProviderAccountKeyFactory.Create(identifier).Match(key => key, _ => throw new InvalidOperationException("The test key must be valid."));

    private static AccountRegistration Registration(string identifier, string displayName) => AccountRegistrationFactory.Create(Key(identifier), CloudProviderFactory.CreateOneDrive(), displayName, $"{displayName.ToLowerInvariant()}@example.com", new DriveId("8A3F2C1D0E9B7A65"), StorageUsageFactory.Create(1_024, 5_368_709_120));
}
