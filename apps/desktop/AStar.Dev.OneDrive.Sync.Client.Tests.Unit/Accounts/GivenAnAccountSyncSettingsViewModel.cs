using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Accounts;

public sealed class GivenAnAccountSyncSettingsViewModel
{
    private const string AccountIdValue = "test-account-id";
    private const string DisplayNameValue = "Test User";
    private const string EmailValue = "test@outlook.com";
    private const string SyncPathValue = "/home/user/OneDrive";

    private static OneDriveAccount BuildAccount(string? localSyncPath = null, ConflictPolicy conflictPolicy = ConflictPolicy.Ignore, int accentIndex = 0) => new()
    {
        Id = new AccountId(AccountIdValue),
        DisplayName = DisplayNameValue,
        Email = EmailValue,
        AccentIndex = accentIndex,
        LocalSyncPath = localSyncPath is null ? null : LocalSyncPath.Restore(localSyncPath),
        ConflictPolicy = conflictPolicy
    };

    private static AccountEntity BuildEntity(string localSyncPath = "") => new()
    {
        Id = new AccountId(AccountIdValue),
        DisplayName = DisplayNameValue,
        Email = EmailValue
    };

    [Fact]
    public void when_constructed_then_account_id_returns_inner_string_value()
    {
        var repository = Substitute.For<IAccountRepository>();
        var sut = new AccountSyncSettingsViewModel(BuildAccount(), repository);

        sut.AccountId.ShouldBe(AccountIdValue);
    }

    [Fact]
    public void when_constructed_then_email_reflects_account_email()
    {
        var repository = Substitute.For<IAccountRepository>();
        var sut = new AccountSyncSettingsViewModel(BuildAccount(), repository);

        sut.Email.ShouldBe(EmailValue);
    }

    [Fact]
    public void when_constructed_then_display_name_reflects_account_display_name()
    {
        var repository = Substitute.For<IAccountRepository>();
        var sut = new AccountSyncSettingsViewModel(BuildAccount(), repository);

        sut.DisplayName.ShouldBe(DisplayNameValue);
    }

    [Fact]
    public void when_accent_index_is_zero_then_accent_hex_returns_first_palette_colour()
    {
        var repository = Substitute.For<IAccountRepository>();
        var sut = new AccountSyncSettingsViewModel(BuildAccount(accentIndex: 0), repository);

        sut.AccentHex.ShouldBe("#185FA5");
    }

    [Fact]
    public void when_accent_index_wraps_past_palette_length_then_accent_hex_returns_first_palette_colour()
    {
        var repository = Substitute.For<IAccountRepository>();
        var sut = new AccountSyncSettingsViewModel(BuildAccount(accentIndex: 6), repository);

        sut.AccentHex.ShouldBe("#185FA5");
    }

    [Fact]
    public void when_account_has_a_local_sync_path_then_local_sync_path_property_is_initialised_from_it()
    {
        var repository = Substitute.For<IAccountRepository>();
        var sut = new AccountSyncSettingsViewModel(BuildAccount(localSyncPath: SyncPathValue), repository);

        sut.LocalSyncPath.ShouldBe(SyncPathValue);
    }

    [Fact]
    public void when_account_has_no_local_sync_path_then_local_sync_path_property_is_empty_string()
    {
        var repository = Substitute.For<IAccountRepository>();
        var sut = new AccountSyncSettingsViewModel(BuildAccount(localSyncPath: null), repository);

        sut.LocalSyncPath.ShouldBe(string.Empty);
    }

    [Fact]
    public void when_constructed_then_conflict_policy_is_initialised_from_account()
    {
        var repository = Substitute.For<IAccountRepository>();
        var sut = new AccountSyncSettingsViewModel(BuildAccount(conflictPolicy: ConflictPolicy.LastWriteWins), repository);

        sut.ConflictPolicy.ShouldBe(ConflictPolicy.LastWriteWins);
    }

    [Fact]
    public void when_constructed_then_policy_options_contains_exactly_five_entries()
    {
        var repository = Substitute.For<IAccountRepository>();
        var sut = new AccountSyncSettingsViewModel(BuildAccount(), repository);

        sut.PolicyOptions.Count.ShouldBe(5);
    }

    [Fact]
    public void when_constructed_then_policy_options_covers_all_conflict_policy_values()
    {
        var repository = Substitute.For<IAccountRepository>();
        var sut = new AccountSyncSettingsViewModel(BuildAccount(), repository);

        var policies = sut.PolicyOptions.Select(option => option.Policy).ToList();
        policies.ShouldContain(ConflictPolicy.Ignore);
        policies.ShouldContain(ConflictPolicy.KeepBoth);
        policies.ShouldContain(ConflictPolicy.LastWriteWins);
        policies.ShouldContain(ConflictPolicy.LocalWins);
        policies.ShouldContain(ConflictPolicy.RemoteWins);
    }

    [Fact]
    public async Task when_browse_command_is_executed_then_no_exception_is_thrown()
    {
        var repository = Substitute.For<IAccountRepository>();
        var sut = new AccountSyncSettingsViewModel(BuildAccount(), repository);

        await sut.BrowseCommand.ExecuteAsync(null);
    }

    [Fact]
    public async Task when_save_is_executed_and_entity_is_not_found_then_upsert_is_not_called()
    {
        var repository = Substitute.For<IAccountRepository>();
        repository.GetByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns((AccountEntity?)null);
        var sut = new AccountSyncSettingsViewModel(BuildAccount(localSyncPath: SyncPathValue), repository);

        await sut.SaveCommand.ExecuteAsync(null);

        await repository.DidNotReceive().UpsertAsync(Arg.Any<AccountEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_save_is_executed_with_a_valid_path_and_entity_exists_then_upsert_is_called_once()
    {
        var repository = Substitute.For<IAccountRepository>();
        repository.GetByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns(BuildEntity());
        var sut = new AccountSyncSettingsViewModel(BuildAccount(), repository);
        sut.LocalSyncPath = SyncPathValue;

        await sut.SaveCommand.ExecuteAsync(null);

        await repository.Received(1).UpsertAsync(Arg.Any<AccountEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_save_is_executed_with_a_valid_path_then_entity_local_sync_path_value_is_updated()
    {
        var repository = Substitute.For<IAccountRepository>();
        repository.GetByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns(BuildEntity());
        AccountEntity? captured = null;
        await repository.UpsertAsync(Arg.Do<AccountEntity>(entity => captured = entity), Arg.Any<CancellationToken>());
        var sut = new AccountSyncSettingsViewModel(BuildAccount(), repository);
        sut.LocalSyncPath = SyncPathValue;

        await sut.SaveCommand.ExecuteAsync(null);

        captured.ShouldNotBeNull();
        captured!.LocalSyncPath.Value.ShouldBe(SyncPathValue);
    }

    [Fact]
    public async Task when_save_is_executed_with_a_valid_path_then_entity_conflict_policy_is_updated()
    {
        var repository = Substitute.For<IAccountRepository>();
        repository.GetByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns(BuildEntity());
        AccountEntity? captured = null;
        await repository.UpsertAsync(Arg.Do<AccountEntity>(entity => captured = entity), Arg.Any<CancellationToken>());
        var sut = new AccountSyncSettingsViewModel(BuildAccount(), repository);
        sut.LocalSyncPath = SyncPathValue;
        sut.ConflictPolicy = ConflictPolicy.RemoteWins;

        await sut.SaveCommand.ExecuteAsync(null);

        captured.ShouldNotBeNull();
        captured!.ConflictPolicy.ShouldBe(ConflictPolicy.RemoteWins);
    }

    [Fact]
    public async Task when_save_is_executed_with_a_valid_path_then_account_model_local_sync_path_is_non_null_with_correct_value()
    {
        var repository = Substitute.For<IAccountRepository>();
        repository.GetByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns(BuildEntity());
        var account = BuildAccount();
        var sut = new AccountSyncSettingsViewModel(account, repository);
        sut.LocalSyncPath = SyncPathValue;

        await sut.SaveCommand.ExecuteAsync(null);

        account.LocalSyncPath.ShouldNotBeNull();
        account.LocalSyncPath!.Value.ShouldBe(SyncPathValue);
    }

    [Fact]
    public async Task when_save_is_executed_with_an_empty_path_then_upsert_is_still_called()
    {
        var repository = Substitute.For<IAccountRepository>();
        repository.GetByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns(BuildEntity());
        var sut = new AccountSyncSettingsViewModel(BuildAccount(), repository);
        sut.LocalSyncPath = string.Empty;

        await sut.SaveCommand.ExecuteAsync(null);

        await repository.Received(1).UpsertAsync(Arg.Any<AccountEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_save_is_executed_with_an_empty_path_then_entity_local_sync_path_value_is_empty_string()
    {
        var repository = Substitute.For<IAccountRepository>();
        repository.GetByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns(BuildEntity());
        AccountEntity? captured = null;
        await repository.UpsertAsync(Arg.Do<AccountEntity>(entity => captured = entity), Arg.Any<CancellationToken>());
        var sut = new AccountSyncSettingsViewModel(BuildAccount(), repository);
        sut.LocalSyncPath = string.Empty;

        await sut.SaveCommand.ExecuteAsync(null);

        captured.ShouldNotBeNull();
        captured!.LocalSyncPath.Value.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task when_save_is_executed_with_a_whitespace_path_then_entity_local_sync_path_value_is_empty_string()
    {
        var repository = Substitute.For<IAccountRepository>();
        repository.GetByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns(BuildEntity());
        AccountEntity? captured = null;
        await repository.UpsertAsync(Arg.Do<AccountEntity>(entity => captured = entity), Arg.Any<CancellationToken>());
        var sut = new AccountSyncSettingsViewModel(BuildAccount(), repository);
        sut.LocalSyncPath = "   ";

        await sut.SaveCommand.ExecuteAsync(null);

        captured.ShouldNotBeNull();
        captured!.LocalSyncPath.Value.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task when_save_is_executed_with_an_empty_path_then_account_model_local_sync_path_is_null()
    {
        var repository = Substitute.For<IAccountRepository>();
        repository.GetByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns(BuildEntity());
        var account = BuildAccount();
        var sut = new AccountSyncSettingsViewModel(account, repository);
        sut.LocalSyncPath = string.Empty;

        await sut.SaveCommand.ExecuteAsync(null);

        account.LocalSyncPath.ShouldBeNull();
    }
}
