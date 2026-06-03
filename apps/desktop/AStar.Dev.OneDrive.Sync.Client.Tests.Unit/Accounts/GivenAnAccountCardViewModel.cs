using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using Avalonia.Media;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Accounts;

public sealed class GivenAnAccountCardViewModel
{
    [Fact]
    public void when_constructed_then_is_active_is_synchronized_from_model()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-1"), IsActive = true };
        var localization = Substitute.For<ILocalizationService>();

        var sut = new AccountCardViewModel(model, localization);

        sut.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void when_constructed_with_inactive_model_then_is_active_is_false()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-2"), IsActive = false };
        var localization = Substitute.For<ILocalizationService>();

        var sut = new AccountCardViewModel(model, localization);

        sut.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void when_constructed_with_no_sync_history_then_localization_receives_never_synced_key()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-3"), LastSyncedAt = null };
        var localization = Substitute.For<ILocalizationService>();

        _ = new AccountCardViewModel(model, localization);

        localization.Received(1).GetLocal("Common.NeverSynced");
    }

    [Fact]
    public void when_constructed_with_recent_sync_within_two_minutes_then_localization_receives_just_now_key()
    {
        var model = new OneDriveAccount
        {
            Id = new AccountId("account-4"),
            LastSyncedAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
        var localization = Substitute.For<ILocalizationService>();

        _ = new AccountCardViewModel(model, localization);

        localization.Received(1).GetLocal("Common.JustNow");
    }

    [Fact]
    public void when_last_synced_thirty_minutes_ago_then_localization_receives_minutes_ago_key()
    {
        var model = new OneDriveAccount
        {
            Id = new AccountId("account-5"),
            LastSyncedAt = DateTimeOffset.UtcNow.AddMinutes(-30)
        };
        var localization = Substitute.For<ILocalizationService>();

        _ = new AccountCardViewModel(model, localization);

        localization.Received(1).GetLocal("Common.MinutesAgo", Arg.Any<object[]>());
    }

    [Fact]
    public void when_last_synced_three_hours_ago_then_localization_receives_hours_ago_key()
    {
        var model = new OneDriveAccount
        {
            Id = new AccountId("account-6"),
            LastSyncedAt = DateTimeOffset.UtcNow.AddHours(-3)
        };
        var localization = Substitute.For<ILocalizationService>();

        _ = new AccountCardViewModel(model, localization);

        localization.Received(1).GetLocal("Common.HoursAgo", Arg.Any<object[]>());
    }

    [Fact]
    public void when_last_synced_yesterday_then_localization_receives_yesterday_key()
    {
        var model = new OneDriveAccount
        {
            Id = new AccountId("account-7"),
            LastSyncedAt = DateTimeOffset.UtcNow.AddHours(-24)
        };
        var localization = Substitute.For<ILocalizationService>();

        _ = new AccountCardViewModel(model, localization);

        localization.Received(1).GetLocal("Common.Yesterday");
    }

    [Fact]
    public void when_last_synced_five_days_ago_then_localization_receives_days_ago_key()
    {
        var model = new OneDriveAccount
        {
            Id = new AccountId("account-8"),
            LastSyncedAt = DateTimeOffset.UtcNow.AddDays(-5)
        };
        var localization = Substitute.For<ILocalizationService>();

        _ = new AccountCardViewModel(model, localization);

        localization.Received(1).GetLocal("Common.DaysAgo", Arg.Any<object[]>());
    }

    [Fact]
    public void when_select_command_is_executed_then_selected_event_is_raised()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-9") };
        var localization = Substitute.For<ILocalizationService>();
        var sut = new AccountCardViewModel(model, localization);
        bool eventRaised = false;
        AccountCardViewModel? raisedViewModel = null;

        sut.Selected += (sender, viewModel) =>
        {
            eventRaised = true;
            raisedViewModel = viewModel;
        };

        sut.SelectCommand.Execute(null);

        eventRaised.ShouldBeTrue();
        raisedViewModel.ShouldBeSameAs(sut);
    }

    [Fact]
    public void when_remove_command_is_executed_then_remove_requested_event_is_raised()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-10") };
        var localization = Substitute.For<ILocalizationService>();
        var sut = new AccountCardViewModel(model, localization);
        bool eventRaised = false;
        AccountCardViewModel? raisedViewModel = null;

        sut.RemoveRequested += (sender, viewModel) =>
        {
            eventRaised = true;
            raisedViewModel = viewModel;
        };

        sut.RemoveCommand.Execute(null);

        eventRaised.ShouldBeTrue();
        raisedViewModel.ShouldBeSameAs(sut);
    }

    [Fact]
    public void when_refresh_from_model_called_with_updated_is_active_then_is_active_is_updated()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-11"), IsActive = true };
        var localization = Substitute.For<ILocalizationService>();
        var sut = new AccountCardViewModel(model, localization);

        model.IsActive = false;
        sut.RefreshFromModel();

        sut.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void when_refresh_from_model_called_then_display_name_property_changed_is_raised()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-12"), Profile = AccountProfileFactory.Create("Alice", string.Empty) };
        var localization = Substitute.For<ILocalizationService>();
        var sut = new AccountCardViewModel(model, localization);
        bool propertyChangedRaised = false;

        sut.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(sut.DisplayName))
                propertyChangedRaised = true;
        };

        sut.RefreshFromModel();

        propertyChangedRaised.ShouldBeTrue();
    }

    [Fact]
    public void when_refresh_from_model_called_then_email_property_changed_is_raised()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-13"), Profile = AccountProfileFactory.Create(string.Empty, "alice@outlook.com") };
        var localization = Substitute.For<ILocalizationService>();
        var sut = new AccountCardViewModel(model, localization);
        bool propertyChangedRaised = false;

        sut.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(sut.Email))
                propertyChangedRaised = true;
        };

        sut.RefreshFromModel();

        propertyChangedRaised.ShouldBeTrue();
    }

    [Fact]
    public void when_refresh_from_model_called_then_initials_property_changed_is_raised()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-14"), Profile = AccountProfileFactory.Create("Bob Smith", string.Empty) };
        var localization = Substitute.For<ILocalizationService>();
        var sut = new AccountCardViewModel(model, localization);
        bool propertyChangedRaised = false;

        sut.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(sut.Initials))
                propertyChangedRaised = true;
        };

        sut.RefreshFromModel();

        propertyChangedRaised.ShouldBeTrue();
    }

    [Fact]
    public void when_is_active_is_changed_then_property_changed_is_raised()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-15"), IsActive = false };
        var localization = Substitute.For<ILocalizationService>();
        var sut = new AccountCardViewModel(model, localization);
        bool propertyChangedRaised = false;

        sut.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(sut.IsActive))
                propertyChangedRaised = true;
        };

        sut.IsActive = true;

        propertyChangedRaised.ShouldBeTrue();
    }

    [Fact]
    public void when_sync_state_is_changed_then_property_changed_is_raised()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-16") };
        var localization = Substitute.For<ILocalizationService>();
        var sut = new AccountCardViewModel(model, localization);
        bool propertyChangedRaised = false;

        sut.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(sut.SyncState))
                propertyChangedRaised = true;
        };

        sut.SyncState = SyncState.Syncing;

        propertyChangedRaised.ShouldBeTrue();
    }

    [Fact]
    public void when_conflict_count_is_changed_then_property_changed_is_raised()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-17") };
        var localization = Substitute.For<ILocalizationService>();
        var sut = new AccountCardViewModel(model, localization);
        bool propertyChangedRaised = false;

        sut.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(sut.ConflictCount))
                propertyChangedRaised = true;
        };

        sut.ConflictCount = 3;

        propertyChangedRaised.ShouldBeTrue();
    }

    [Fact]
    public void when_last_sync_text_is_changed_then_property_changed_is_raised()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-18") };
        var localization = Substitute.For<ILocalizationService>();
        var sut = new AccountCardViewModel(model, localization);
        bool propertyChangedRaised = false;

        sut.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(sut.LastSyncText))
                propertyChangedRaised = true;
        };

        sut.LastSyncText = "2h ago";

        propertyChangedRaised.ShouldBeTrue();
    }

    [Fact]
    public void when_display_name_has_two_parts_then_initials_are_first_letter_of_each()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-19"), Profile = AccountProfileFactory.Create("Alice Smith", string.Empty) };
        var localization = Substitute.For<ILocalizationService>();

        var sut = new AccountCardViewModel(model, localization);

        sut.Initials.ShouldBe("AS");
    }

    [Fact]
    public void when_display_name_is_single_word_then_initials_are_first_letter()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-20"), Profile = AccountProfileFactory.Create("Alice", string.Empty) };
        var localization = Substitute.For<ILocalizationService>();

        var sut = new AccountCardViewModel(model, localization);

        sut.Initials.ShouldBe("A");
    }

    [Fact]
    public void when_display_name_is_empty_and_email_provided_then_initials_are_first_letter_of_email()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-21"), Profile = AccountProfileFactory.Create(string.Empty, "bob@outlook.com") };
        var localization = Substitute.For<ILocalizationService>();

        var sut = new AccountCardViewModel(model, localization);

        sut.Initials.ShouldBe("B");
    }

    [Fact]
    public void when_display_name_and_email_are_empty_then_initials_are_question_mark()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-22"), Profile = AccountProfileFactory.Create(string.Empty, string.Empty) };
        var localization = Substitute.For<ILocalizationService>();

        var sut = new AccountCardViewModel(model, localization);

        sut.Initials.ShouldBe("?");
    }

    [Fact]
    public void when_display_name_has_multiple_words_then_initials_use_first_and_last()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-23"), Profile = AccountProfileFactory.Create("John Michael Smith", string.Empty) };
        var localization = Substitute.For<ILocalizationService>();

        var sut = new AccountCardViewModel(model, localization);

        sut.Initials.ShouldBe("JS");
    }

    [Fact]
    public void when_initials_are_derived_from_display_name_then_they_are_uppercase()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-24"), Profile = AccountProfileFactory.Create("alice smith", string.Empty) };
        var localization = Substitute.For<ILocalizationService>();

        var sut = new AccountCardViewModel(model, localization);

        sut.Initials.ShouldBe("AS");
    }

    [Fact]
    public void when_initials_are_derived_from_email_then_they_are_uppercase()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-25"), Profile = AccountProfileFactory.Create(string.Empty, "alice@outlook.com") };
        var localization = Substitute.For<ILocalizationService>();

        var sut = new AccountCardViewModel(model, localization);

        sut.Initials.ShouldBe("A");
    }

    [Fact]
    public void when_accent_index_is_zero_then_accent_hex_returns_first_palette_color()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-26"), AccentIndex = 0 };
        var localization = Substitute.For<ILocalizationService>();

        var sut = new AccountCardViewModel(model, localization);

        sut.AccentHex.ShouldBe("#185FA5");
    }

    [Fact]
    public void when_accent_index_is_beyond_palette_length_then_accent_hex_wraps_around()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-27"), AccentIndex = 6 };
        var localization = Substitute.For<ILocalizationService>();

        var sut = new AccountCardViewModel(model, localization);

        sut.AccentHex.ShouldBe("#185FA5");
    }

    [Fact]
    public void when_accent_index_is_five_then_accent_hex_returns_last_palette_color()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-28"), AccentIndex = 5 };
        var localization = Substitute.For<ILocalizationService>();

        var sut = new AccountCardViewModel(model, localization);

        sut.AccentHex.ShouldBe("#854F0B");
    }

    [Fact]
    public void when_accent_color_is_queried_then_it_returns_parsed_color_from_hex()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-29"), AccentIndex = 0 };
        var localization = Substitute.For<ILocalizationService>();

        var sut = new AccountCardViewModel(model, localization);

        var expectedColor = Color.Parse("#185FA5");
        sut.AccentColor.ShouldBe(expectedColor);
    }

    [Fact]
    public void when_last_synced_exactly_two_minutes_ago_then_localization_receives_minutes_ago_key_not_just_now()
    {
        var model = new OneDriveAccount
        {
            Id = new AccountId("account-31"),
            LastSyncedAt = DateTimeOffset.UtcNow.AddMinutes(-2)
        };
        var localization = Substitute.For<ILocalizationService>();

        _ = new AccountCardViewModel(model, localization);

        localization.Received(1).GetLocal("Common.MinutesAgo", Arg.Any<object[]>());
        localization.DidNotReceive().GetLocal("Common.JustNow");
    }

    [Fact]
    public void when_last_synced_exactly_one_hour_ago_then_localization_receives_hours_ago_key_not_minutes()
    {
        var model = new OneDriveAccount
        {
            Id = new AccountId("account-32"),
            LastSyncedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };
        var localization = Substitute.For<ILocalizationService>();

        _ = new AccountCardViewModel(model, localization);

        localization.Received(1).GetLocal("Common.HoursAgo", Arg.Any<object[]>());
        localization.DidNotReceive().GetLocal("Common.MinutesAgo", Arg.Any<object[]>());
    }

    [Fact]
    public void when_last_synced_exactly_one_day_ago_then_localization_receives_yesterday_key_not_hours()
    {
        var model = new OneDriveAccount
        {
            Id = new AccountId("account-33"),
            LastSyncedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        var localization = Substitute.For<ILocalizationService>();

        _ = new AccountCardViewModel(model, localization);

        localization.Received(1).GetLocal("Common.Yesterday");
        localization.DidNotReceive().GetLocal("Common.HoursAgo", Arg.Any<object[]>());
    }

    [Fact]
    public void when_last_synced_exactly_two_days_ago_then_localization_receives_days_ago_key_not_yesterday()
    {
        var model = new OneDriveAccount
        {
            Id = new AccountId("account-34"),
            LastSyncedAt = DateTimeOffset.UtcNow.AddDays(-2)
        };
        var localization = Substitute.For<ILocalizationService>();

        _ = new AccountCardViewModel(model, localization);

        localization.Received(1).GetLocal("Common.DaysAgo", Arg.Any<object[]>());
        localization.DidNotReceive().GetLocal("Common.Yesterday");
    }

    [Fact]
    public void when_constructed_with_sync_within_two_minutes_then_just_now_key_is_looked_up()
    {
        var model = new OneDriveAccount
        {
            Id = new AccountId("account-35a"),
            LastSyncedAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
        var localization = Substitute.For<ILocalizationService>();

        _ = new AccountCardViewModel(model, localization);

        localization.Received(1).GetLocal("Common.JustNow");
    }

    [Fact]
    public void when_refresh_from_model_is_called_after_time_passes_then_minutes_ago_key_is_looked_up()
    {
        var model = new OneDriveAccount
        {
            Id = new AccountId("account-35b"),
            LastSyncedAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
        var localization = Substitute.For<ILocalizationService>();
        var sut = new AccountCardViewModel(model, localization);

        model.LastSyncedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        sut.RefreshFromModel();

        localization.Received(1).GetLocal("Common.MinutesAgo", Arg.Any<object[]>());
    }

    [Fact]
    public void when_display_name_contains_extra_whitespace_then_initials_ignore_empty_parts()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-36"), Profile = AccountProfileFactory.Create("Alice    Smith", string.Empty) };
        var localization = Substitute.For<ILocalizationService>();

        var sut = new AccountCardViewModel(model, localization);

        sut.Initials.ShouldBe("AS");
    }
}
