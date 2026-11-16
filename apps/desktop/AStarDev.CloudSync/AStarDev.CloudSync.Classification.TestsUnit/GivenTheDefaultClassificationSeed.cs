using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenTheDefaultClassificationSeed
{
    private static readonly IReadOnlyDictionary<string, string[]> ExpectedExtensions = new Dictionary<string, string[]>
    {
        ["Document"] = ["doc", "docx", "odt", "rtf", "txt", "md"],
        ["Pdf"] = ["pdf"],
        ["Spreadsheet"] = ["xls", "xlsx", "ods", "csv"],
        ["Presentation"] = ["ppt", "pptx", "odp"],
        ["Photo"] = ["jpg", "jpeg", "png", "gif", "heic", "webp", "bmp"],
        ["Video"] = ["mp4", "mkv", "mov", "avi", "wmv"],
        ["Audio"] = ["mp3", "flac", "wav", "m4a"],
        ["Archive"] = ["zip", "7z", "rar", "tar", "gz"],
        ["Code"] = ["cs", "ps1", "sh", "json", "xml", "py", "js"],
        ["Installer"] = ["exe", "msi", "iso", "appimage", "deb", "rpm"],
    };

    [Fact]
    public async Task when_the_default_seed_is_loaded_then_every_default_category_exists()
    {
        var seed = await LoadAsync();

        ExpectedExtensions.Keys.Append("Unclassified").ShouldBeSubsetOf(seed.Categories.Select(category => category.Name).ToList());
    }

    [Fact]
    public async Task when_the_default_seed_is_loaded_then_every_extension_maps_to_its_category_once()
    {
        var seed = await LoadAsync();

        foreach (var (category, extensions) in ExpectedExtensions)
        {
            var actual = seed.Terms.Where(term => term.Kind is SeedTermKind.Extension && term.CategoryName == category).Select(term => term.Text);
            actual.ShouldBe(extensions, ignoreOrder: true);
        }
    }

    [Fact]
    public async Task when_the_default_seed_is_loaded_then_no_extension_repeats_ignoring_case()
    {
        var seed = await LoadAsync();

        var extensions = seed.Terms.Where(term => term.Kind is SeedTermKind.Extension).Select(term => term.Text).ToList();

        extensions.Count.ShouldBe(extensions.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        extensions.ShouldAllBe(extension => extension.All(character => !char.IsUpper(character) && character != '.'));
    }

    [Theory]
    [InlineData("documents", "Document")]
    [InlineData("pictures", "Photo")]
    [InlineData("photos", "Photo")]
    [InlineData("videos", "Video")]
    [InlineData("music", "Audio")]
    [InlineData("downloads", "Unclassified")]
    public async Task when_the_default_seed_is_loaded_then_the_folder_name_maps_to_its_category(string folder, string category)
    {
        var seed = await LoadAsync();

        seed.Terms.Where(term => term.Kind is SeedTermKind.Folder && term.Text == folder).Select(term => term.CategoryName).ShouldBe([category]);
    }

    [Fact]
    public async Task when_the_default_seed_is_loaded_then_no_term_is_dropped_for_an_unknown_category()
    {
        var seed = await LoadAsync();

        seed.Terms.Count.ShouldBeGreaterThan(ExpectedExtensions.Values.Sum(extensions => extensions.Length));
    }

    private static async Task<ClassificationSeed> LoadAsync() =>
        await EmbeddedClassificationSeedSourceFactory.CreateDefault().LoadAsync(TestContext.Current.CancellationToken).MatchAsync(seed => seed, _ => throw new InvalidOperationException());
}
