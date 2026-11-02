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

    private static string ReadValue(string culture, string key)
    {
        using var stream = typeof(LocalizationService).Assembly.GetManifestResourceStream($"AStarDev.OneDriveSyncClient.Assets.Localization.{culture}.json");
        using var document = JsonDocument.Parse(stream!);

        return document.RootElement.GetProperty(key).GetString()!;
    }

    [Theory]
    [InlineData("en-GB", "StatusBar.Pending")]
    [InlineData("en-GB", "StatusBar.Conflict")]
    [InlineData("en-GB", "StatusBar.Conflicts")]
    [InlineData("en-GB", "Classifications.ReclassifyProgress")]
    [InlineData("en-GB", "Classifications.ReclassifyComplete")]
    [InlineData("en-GB", "Common.DaysAgo")]
    [InlineData("en-US", "StatusBar.Pending")]
    [InlineData("en-US", "StatusBar.Conflict")]
    [InlineData("en-US", "StatusBar.Conflicts")]
    [InlineData("en-US", "Classifications.ReclassifyProgress")]
    [InlineData("en-US", "Classifications.ReclassifyComplete")]
    [InlineData("en-US", "Common.DaysAgo")]
    public void when_a_count_template_is_read_then_every_placeholder_uses_the_thousands_format(string culture, string key)
    {
        var value = ReadValue(culture, key);

        value.ShouldNotMatch(@"\{\d\}");
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
    [InlineData("Classifications.Reclassify")]
    [InlineData("Classifications.ReclassifyDialogTitle")]
    [InlineData("Classifications.ReclassifyConfirmMessage")]
    [InlineData("Classifications.ReclassifyCancelled")]
    [InlineData("Classifications.ReclassifyProgress")]
    [InlineData("Classifications.ReclassifyComplete")]
    [InlineData("Classifications.ReclassifyFailed")]
    [InlineData("Classifications.CategoryDeleteFailed")]
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
    [InlineData("Wizard.AddAccount.UnexpectedAuthError")]
    [InlineData("Classifications.ExportDialogTitle")]
    [InlineData("Classifications.ImportDialogTitle")]
    [InlineData("Classifications.ImportConfirmMessage")]
    [InlineData("Settings.ChooseSyncFolder")]
    [InlineData("Common.Save")]
    public void when_a_key_is_required_then_both_cultures_define_it(string key)
    {
        ReadKeys("en-GB").ShouldContain(key);
        ReadKeys("en-US").ShouldContain(key);
    }
}
