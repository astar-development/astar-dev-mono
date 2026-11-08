using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.ViewModels.Accounts;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Accounts;

public class GivenAPlaintextCacheConsentSettingsViewModel
{
    private readonly FakePlaintextCacheConsentSettings grantedSettings = new() { Consent = PlaintextCacheConsentFactory.CreateGranted() };

    [Fact]
    public void when_no_consent_is_remembered_then_the_setting_is_hidden()
    {
        var setting = new PlaintextCacheConsentSettingsViewModel(new FakePlaintextCacheConsentSettings(), TestData.CreateLocalization());

        (setting.IsVisible, setting.Step).ShouldBe((false, PlaintextCacheConsentSettingsStepFactory.CreateNotGranted()));
    }

    [Fact]
    public void when_a_consent_is_remembered_then_the_withdraw_button_shows_with_localised_texts()
    {
        var setting = CreateSetting(grantedSettings);

        (setting.IsVisible, setting.IsGranted, setting.IsConfirming, setting.IsShowingResult).ShouldBe((true, true, false, false));
        string[] texts = [setting.HeadingText, setting.GrantedText, setting.WithdrawText, setting.ConfirmMessageText, setting.ConfirmText, setting.CancelText];
        texts.ShouldAllBe(text => !string.IsNullOrWhiteSpace(text) && !text.StartsWith("Settings."));
    }

    [Fact]
    public void when_withdraw_is_chosen_then_the_user_is_asked_to_confirm_and_nothing_is_withdrawn_yet()
    {
        var setting = CreateSetting(grantedSettings);

        setting.WithdrawCommand.Execute(null);

        (setting.IsConfirming, setting.IsGranted, grantedSettings.Withdrawals).ShouldBe((true, false, 0));
    }

    [Fact]
    public void when_the_confirmation_is_cancelled_then_the_consent_is_kept()
    {
        var setting = CreateSetting(grantedSettings);
        setting.WithdrawCommand.Execute(null);

        setting.CancelCommand.Execute(null);

        (setting.IsGranted, grantedSettings.Withdrawals).ShouldBe((true, 0));
    }

    [Fact]
    public async Task when_the_withdrawal_is_confirmed_then_the_consent_is_withdrawn_and_the_user_is_told_to_sign_in_again()
    {
        var setting = CreateSetting(grantedSettings);
        setting.WithdrawCommand.Execute(null);

        await setting.ConfirmCommand.ExecuteAsync(null);

        (setting.Step, setting.IsShowingResult, setting.IsGranted, grantedSettings.Withdrawals).ShouldBe((PlaintextCacheConsentSettingsStepFactory.CreateWithdrawn(), true, false, 1));
        setting.ResultText.ShouldContain("sign in again");
    }

    [Fact]
    public async Task when_the_withdrawal_fails_then_a_failure_shows_and_the_user_can_try_again()
    {
        var settings = new FakePlaintextCacheConsentSettings { Consent = PlaintextCacheConsentFactory.CreateGranted(), WithdrawFails = true };
        var setting = CreateSetting(settings);
        setting.WithdrawCommand.Execute(null);

        await setting.ConfirmCommand.ExecuteAsync(null);

        (setting.Step, setting.IsShowingResult, setting.IsGranted).ShouldBe((PlaintextCacheConsentSettingsStepFactory.CreateFailed(), true, true));
        setting.ResultText.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void when_a_consent_is_granted_after_creation_then_refresh_shows_the_setting()
    {
        var settings = new FakePlaintextCacheConsentSettings();
        var setting = CreateSetting(settings);
        var changed = new List<string?>();
        setting.PropertyChanged += (_, args) => changed.Add(args.PropertyName);
        settings.Consent = PlaintextCacheConsentFactory.CreateGranted();

        setting.Refresh();

        (setting.IsVisible, setting.IsGranted).ShouldBe((true, true));
        changed.ShouldContain(nameof(PlaintextCacheConsentSettingsViewModel.IsVisible));
    }

    [Fact]
    public void when_nothing_was_withdrawn_then_the_result_text_is_empty()
    {
        CreateSetting(grantedSettings).ResultText.ShouldBeEmpty();
    }

    private static PlaintextCacheConsentSettingsViewModel CreateSetting(FakePlaintextCacheConsentSettings settings) => new(settings, TestData.CreateLocalization());
}
