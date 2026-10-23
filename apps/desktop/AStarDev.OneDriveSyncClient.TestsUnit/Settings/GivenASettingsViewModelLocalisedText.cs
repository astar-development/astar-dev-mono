using System.Globalization;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Autostart;
using AStarDev.OneDriveSyncClient.Infrastructure.Shell;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using AStarDev.OneDriveSyncClient.Infrastructure.Theme;
using AStarDev.OneDriveSyncClient.Localization;
using AStarDev.OneDriveSyncClient.Settings;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Settings;

public sealed class GivenASettingsViewModelLocalisedText
{
    private readonly ILocalizationService loc = Substitute.For<ILocalizationService>();
    private readonly SettingsViewModel sut;

    public GivenASettingsViewModelLocalisedText()
    {
        var settingsService = Substitute.For<ISettingsService>();
        settingsService.Current.Returns(new AppSettings());
        loc.GetLocal(Arg.Any<string>()).Returns(x => x.ArgAt<string>(0));
        loc.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns(x => x.ArgAt<string>(0));
        loc.AvailableCultures.Returns([CultureInfo.GetCultureInfo("en-GB")]);
        sut = new SettingsViewModel(settingsService, Substitute.For<IThemeService>(), Substitute.For<ISyncScheduler>(), Substitute.For<IAccountRepository>(), loc, Substitute.For<IFolderPickerService>(), Substitute.For<IAutostartService>());
    }

    public static TheoryData<string, string> TextProperties => new()
    {
        { "TitleText", "Settings.Title" },
        { "DescriptionText", "Settings.Description" },
        { "AppearanceHeadingText", "Settings.Section.Appearance" },
        { "ThemeLabelText", "Settings.Theme" },
        { "ThemeDescriptionText", "Settings.Theme.Description" },
        { "LanguageHeadingText", "Settings.Section.Language" },
        { "DisplayLanguageLabelText", "Settings.Language.DisplayLanguage" },
        { "SyncPolicyHeadingText", "Settings.Section.SyncPolicy" },
        { "DefaultConflictPolicyLabelText", "Settings.DefaultConflictPolicy" },
        { "DefaultConflictPolicyDescriptionText", "Settings.DefaultConflictPolicy.Description" },
        { "SyncIntervalLabelText", "Settings.DeltaSyncInterval" },
        { "SyncIntervalDescriptionText", "Settings.DeltaSyncInterval.Description" },
        { "ConcurrentWorkersLabelText", "Settings.ConcurrentWorkers.Label" },
        { "ConcurrentWorkersDescriptionText", "Settings.ConcurrentWorkers.Description" },
        { "AccountSyncPathsHeadingText", "Settings.Section.AccountSyncPaths" },
        { "AccountSyncPathsDescriptionText", "Settings.Section.AccountSyncPaths.Description" },
    };

    private string? ReadText(string propertyName) => (string?)typeof(SettingsViewModel).GetProperty(propertyName)?.GetValue(sut);

    [Theory]
    [MemberData(nameof(TextProperties))]
    public void when_text_property_is_read_then_it_comes_from_the_localisation_service(string propertyName, string key) => ReadText(propertyName).ShouldBe(key);

    [Theory]
    [MemberData(nameof(TextProperties))]
    public void when_culture_changes_then_text_property_change_is_raised(string propertyName, string key)
    {
        var raised = new List<string?>();
        sut.PropertyChanged += (_, args) => raised.Add(args.PropertyName);

        loc.CultureChanged += Raise.Event<EventHandler<CultureInfo>>(loc, CultureInfo.GetCultureInfo("en-US"));

        raised.ShouldContain(propertyName);
        key.ShouldNotBeEmpty();
    }
}
