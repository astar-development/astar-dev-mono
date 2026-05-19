using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using Avalonia.Media;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Accounts;

public sealed class GivenAnAccountCardViewModel
{
    [Fact]
    public void when_constructed_then_is_active_is_synchronized_from_model()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-1"), IsActive = true };

        var sut = new AccountCardViewModel(model);

        sut.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void when_constructed_with_inactive_model_then_is_active_is_false()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-2"), IsActive = false };

        var sut = new AccountCardViewModel(model);

        sut.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void when_constructed_with_no_sync_history_then_last_sync_text_is_never_synced()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-3"), LastSyncedAt = null };

        var sut = new AccountCardViewModel(model);

        sut.LastSyncText.ShouldBe("Never synced");
    }

    [Fact]
    public void when_constructed_with_recent_sync_within_two_minutes_then_last_sync_text_is_just_now()
    {
        var model = new OneDriveAccount
        {
            Id = new AccountId("account-4"),
            LastSyncedAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        };

        var sut = new AccountCardViewModel(model);

        sut.LastSyncText.ShouldBe("Just now");
    }

    [Fact]
    public void when_last_synced_thirty_minutes_ago_then_last_sync_text_shows_minutes()
    {
        var model = new OneDriveAccount
        {
            Id = new AccountId("account-5"),
            LastSyncedAt = DateTimeOffset.UtcNow.AddMinutes(-30)
        };

        var sut = new AccountCardViewModel(model);

        sut.LastSyncText.ShouldBe("30m ago");
    }

    [Fact]
    public void when_last_synced_three_hours_ago_then_last_sync_text_shows_hours()
    {
        var model = new OneDriveAccount
        {
            Id = new AccountId("account-6"),
            LastSyncedAt = DateTimeOffset.UtcNow.AddHours(-3)
        };

        var sut = new AccountCardViewModel(model);

        sut.LastSyncText.ShouldBe("3h ago");
    }

    [Fact]
    public void when_last_synced_yesterday_then_last_sync_text_is_yesterday()
    {
        var model = new OneDriveAccount
        {
            Id = new AccountId("account-7"),
            LastSyncedAt = DateTimeOffset.UtcNow.AddHours(-24)
        };

        var sut = new AccountCardViewModel(model);

        sut.LastSyncText.ShouldBe("Yesterday");
    }

    [Fact]
    public void when_last_synced_five_days_ago_then_last_sync_text_shows_days()
    {
        var model = new OneDriveAccount
        {
            Id = new AccountId("account-8"),
            LastSyncedAt = DateTimeOffset.UtcNow.AddDays(-5)
        };

        var sut = new AccountCardViewModel(model);

        sut.LastSyncText.ShouldBe("5d ago");
    }

    [Fact]
    public void when_select_command_is_executed_then_selected_event_is_raised()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-9") };
        var sut = new AccountCardViewModel(model);
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
        var sut = new AccountCardViewModel(model);
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
        var sut = new AccountCardViewModel(model);

        model.IsActive = false;
        sut.RefreshFromModel();

        sut.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void when_refresh_from_model_called_then_display_name_property_changed_is_raised()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-12"), Profile = AccountProfileFactory.Create("Alice", string.Empty) };
        var sut = new AccountCardViewModel(model);
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
        var sut = new AccountCardViewModel(model);
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
        var sut = new AccountCardViewModel(model);
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
        var sut = new AccountCardViewModel(model);
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
        var sut = new AccountCardViewModel(model);
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
        var sut = new AccountCardViewModel(model);
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
        var sut = new AccountCardViewModel(model);
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

        var sut = new AccountCardViewModel(model);

        sut.Initials.ShouldBe("AS");
    }

    [Fact]
    public void when_display_name_is_single_word_then_initials_are_first_letter()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-20"), Profile = AccountProfileFactory.Create("Alice", string.Empty) };

        var sut = new AccountCardViewModel(model);

        sut.Initials.ShouldBe("A");
    }

    [Fact]
    public void when_display_name_is_empty_and_email_provided_then_initials_are_first_letter_of_email()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-21"), Profile = AccountProfileFactory.Create(string.Empty, "bob@outlook.com") };

        var sut = new AccountCardViewModel(model);

        sut.Initials.ShouldBe("B");
    }

    [Fact]
    public void when_display_name_and_email_are_empty_then_initials_are_question_mark()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-22"), Profile = AccountProfileFactory.Create(string.Empty, string.Empty) };

        var sut = new AccountCardViewModel(model);

        sut.Initials.ShouldBe("?");
    }

    [Fact]
    public void when_display_name_has_multiple_words_then_initials_use_first_and_last()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-23"), Profile = AccountProfileFactory.Create("John Michael Smith", string.Empty) };

        var sut = new AccountCardViewModel(model);

        sut.Initials.ShouldBe("JS");
    }

    [Fact]
    public void when_initials_are_derived_from_display_name_then_they_are_uppercase()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-24"), Profile = AccountProfileFactory.Create("alice smith", string.Empty) };

        var sut = new AccountCardViewModel(model);

        sut.Initials.ShouldBe("AS");
    }

    [Fact]
    public void when_initials_are_derived_from_email_then_they_are_uppercase()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-25"), Profile = AccountProfileFactory.Create(string.Empty, "alice@outlook.com") };

        var sut = new AccountCardViewModel(model);

        sut.Initials.ShouldBe("A");
    }

    [Fact]
    public void when_accent_index_is_zero_then_accent_hex_returns_first_palette_color()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-26"), AccentIndex = 0 };

        var sut = new AccountCardViewModel(model);

        sut.AccentHex.ShouldBe("#185FA5");
    }

    [Fact]
    public void when_accent_index_is_beyond_palette_length_then_accent_hex_wraps_around()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-27"), AccentIndex = 6 };

        var sut = new AccountCardViewModel(model);

        sut.AccentHex.ShouldBe("#185FA5");
    }

    [Fact]
    public void when_accent_index_is_five_then_accent_hex_returns_last_palette_color()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-28"), AccentIndex = 5 };

        var sut = new AccountCardViewModel(model);

        sut.AccentHex.ShouldBe("#854F0B");
    }

    [Fact]
    public void when_accent_color_is_queried_then_it_returns_parsed_color_from_hex()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-29"), AccentIndex = 0 };

        var sut = new AccountCardViewModel(model);

        var expectedColor = Color.Parse("#185FA5");
        sut.AccentColor.ShouldBe(expectedColor);
    }

    [Fact]
    public void when_last_synced_exactly_two_minutes_ago_then_text_shows_minutes_not_just_now()
    {
        var model = new OneDriveAccount
        {
            Id = new AccountId("account-31"),
            LastSyncedAt = DateTimeOffset.UtcNow.AddMinutes(-2)
        };

        var sut = new AccountCardViewModel(model);

        sut.LastSyncText.ShouldBe("2m ago");
    }

    [Fact]
    public void when_last_synced_exactly_one_hour_ago_then_text_shows_hours_not_minutes()
    {
        var model = new OneDriveAccount
        {
            Id = new AccountId("account-32"),
            LastSyncedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        var sut = new AccountCardViewModel(model);

        sut.LastSyncText.ShouldBe("1h ago");
    }

    [Fact]
    public void when_last_synced_exactly_one_day_ago_then_text_is_yesterday_not_hours()
    {
        var model = new OneDriveAccount
        {
            Id = new AccountId("account-33"),
            LastSyncedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        var sut = new AccountCardViewModel(model);

        sut.LastSyncText.ShouldBe("Yesterday");
    }

    [Fact]
    public void when_last_synced_exactly_two_days_ago_then_text_shows_days_not_yesterday()
    {
        var model = new OneDriveAccount
        {
            Id = new AccountId("account-34"),
            LastSyncedAt = DateTimeOffset.UtcNow.AddDays(-2)
        };

        var sut = new AccountCardViewModel(model);

        sut.LastSyncText.ShouldBe("2d ago");
    }

    [Fact]
    public void when_refresh_from_model_is_called_then_last_sync_text_is_recalculated()
    {
        var model = new OneDriveAccount
        {
            Id = new AccountId("account-35"),
            LastSyncedAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        };

        var sut = new AccountCardViewModel(model);
        sut.LastSyncText.ShouldBe("Just now");

        // Simulate time passing by updating the model's LastSyncedAt
        model.LastSyncedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        sut.RefreshFromModel();

        sut.LastSyncText.ShouldBe("5m ago");
    }

    [Fact]
    public void when_display_name_contains_extra_whitespace_then_initials_ignore_empty_parts()
    {
        var model = new OneDriveAccount { Id = new AccountId("account-36"), Profile = AccountProfileFactory.Create("Alice    Smith", string.Empty) };

        var sut = new AccountCardViewModel(model);

        sut.Initials.ShouldBe("AS");
    }
}
