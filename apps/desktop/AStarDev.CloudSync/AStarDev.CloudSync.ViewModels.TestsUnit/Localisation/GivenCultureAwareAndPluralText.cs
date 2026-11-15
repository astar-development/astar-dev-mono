using System.Globalization;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.CloudSync.ViewModels.Activity;
using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Localisation;

public sealed class GivenCultureAwareAndPluralText
{
    private static ActivityItemViewModel ItemFor(ActivityKind kind, ILocalizationService localization)
    {
        var entry = ActivityEntryFactory.Create(AccountIdFactory.Create(Guid.NewGuid()), Option.None<SyncPairId>(), kind, TestData.Now);

        return new ActivityItemViewModel(entry, "Captain Flint", new ProviderViewModel(CloudProviderFactory.CreateOneDrive(), localization), localization, TestData.CreateTime());
    }

    private static CultureLocalization In(string culture) => new(TestData.CreateLocalization(), CultureInfo.GetCultureInfo(culture));

    [Theory]
    [InlineData(0, "Downloaded 0 files")]
    [InlineData(1, "Downloaded 1 file")]
    [InlineData(2, "Downloaded 2 files")]
    public void when_files_are_downloaded_then_the_text_is_right_for_zero_one_and_many(int count, string expected)
    {
        ItemFor(ActivityKindFactory.CreateDownloaded(count), TestData.CreateLocalization()).Text.ShouldBe(expected);
    }

    [Theory]
    [InlineData(0, "Deleted 0 files")]
    [InlineData(1, "Deleted 1 file")]
    [InlineData(2, "Deleted 2 files")]
    public void when_files_are_deleted_then_the_text_is_right_for_zero_one_and_many(int count, string expected)
    {
        ItemFor(ActivityKindFactory.CreateDeleted(count), TestData.CreateLocalization()).Text.ShouldBe(expected);
    }

    [Theory]
    [InlineData("en-GB", "Downloaded 1,234 files")]
    [InlineData("de-DE", "Downloaded 1.234 files")]
    [InlineData("fr-FR", "Downloaded 1 234 files")]
    public void when_a_count_is_shown_then_it_follows_the_ui_culture(string culture, string expected)
    {
        ItemFor(ActivityKindFactory.CreateDownloaded(1234), In(culture)).Text.ShouldBe(expected);
    }

    [Theory]
    [InlineData("en-GB", "1.5 KB")]
    [InlineData("de-DE", "1,5 KB")]
    public void when_a_size_is_shown_then_it_follows_the_ui_culture(string culture, string expected)
    {
        ByteSizeFormatter.Format(1536, In(culture)).ShouldBe(expected);
    }

    [Theory]
    [InlineData("en-GB", "1.5 KB")]
    [InlineData("de-DE", "1,5 KB")]
    public void when_an_upload_size_is_shown_then_it_follows_the_ui_culture(string culture, string expected)
    {
        ItemFor(ActivityKindFactory.CreateUploaded(1, 1536), In(culture)).Text.ShouldBe($"Uploaded 1 file ({expected})");
    }
}
