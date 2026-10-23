using System.Text.Json;
using AStarDev.OneDriveSyncClient.Localization;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Localization;

public sealed class GivenTheLocalisationFiles
{
    private static HashSet<string> ReadKeys(string culture)
    {
        using var stream = typeof(LocalizationService).Assembly.GetManifestResourceStream($"AStarDev.OneDriveSyncClient.Assets.Localization.{culture}.json");
        using var document = JsonDocument.Parse(stream!);

        return document.RootElement.EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
    }

    [Fact]
    public void when_en_gb_and_en_us_are_compared_then_they_define_the_same_keys()
    {
        var enGb = ReadKeys("en-GB");
        var enUs = ReadKeys("en-US");

        enGb.SymmetricExceptWith(enUs);

        enGb.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Settings.Description")]
    [InlineData("Settings.Theme.Description")]
    [InlineData("Settings.Language.DisplayLanguage")]
    [InlineData("Settings.DefaultConflictPolicy.Description")]
    [InlineData("Settings.DeltaSyncInterval.Description")]
    [InlineData("Settings.ConcurrentWorkers.Label")]
    [InlineData("Settings.ConcurrentWorkers.Description")]
    [InlineData("Settings.Section.AccountSyncPaths")]
    [InlineData("Settings.Section.AccountSyncPaths.Description")]
    [InlineData("Classifications.Title")]
    [InlineData("Classifications.Description")]
    [InlineData("Classifications.FilterPlaceholder")]
    [InlineData("Classifications.Export")]
    [InlineData("Classifications.Import")]
    [InlineData("Classifications.Empty")]
    [InlineData("Classifications.Famous")]
    [InlineData("Classifications.Internet")]
    [InlineData("Classifications.Edit")]
    [InlineData("Classifications.Delete")]
    [InlineData("Classifications.NewSubCategoryPlaceholder")]
    [InlineData("Classifications.AddSubCategory")]
    [InlineData("Classifications.AddCategory")]
    [InlineData("Classifications.CategoryNamePlaceholder")]
    [InlineData("Classifications.Add")]
    [InlineData("Classifications.EditDialog.Title")]
    [InlineData("Classifications.EditDialog.Name")]
    [InlineData("Classifications.EditDialog.IncludeInSearch")]
    [InlineData("Classifications.EditDialog.Parent")]
    [InlineData("Conflict.Versus")]
    [InlineData("Conflict.Resolution")]
    [InlineData("Conflict.Resolving")]
    [InlineData("Conflict.Dismiss")]
    [InlineData("Conflict.Apply")]
    [InlineData("Files.OpenInFileManager")]
    [InlineData("Files.ViewFolderActivity")]
    [InlineData("Search.Result.Open.Button")]
    [InlineData("Files.LoadDriveIdFailed")]
    [InlineData("Files.LoadFoldersFailed")]
    [InlineData("Wizard.AddAccount.LoadFoldersFailed")]
    public void when_a_key_is_required_then_both_cultures_define_it(string key)
    {
        ReadKeys("en-GB").ShouldContain(key);
        ReadKeys("en-US").ShouldContain(key);
    }
}
