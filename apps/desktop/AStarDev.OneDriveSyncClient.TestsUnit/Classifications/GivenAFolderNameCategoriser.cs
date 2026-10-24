using AStarDev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Classifications;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Classifications;

public sealed class GivenAFolderNameCategoriser
{
    private readonly FolderNameCategoriser sut = new();

    private List<(string Level1, string Level2)> CategoriseToLevels(string remotePath)
        => [.. sut.Categorise(remotePath).Select(classification => (classification.Level1, classification.Level2.MapOrDefault(level2 => level2, string.Empty)))];

    [Theory]
    [InlineData("/Pictures/Wallpapers/forest.jpg", "Photo", "Wallpaper")]
    [InlineData("/Pictures/Screenshots/shot.png", "Photo", "Screenshot")]
    [InlineData("/Finance/Receipts/shop.pdf", "Document", "Receipt")]
    [InlineData("/Finance/Invoices/march.pdf", "Document", "Invoice")]
    [InlineData("/Work/docs/notes.txt", "Document", "")]
    [InlineData("/Pictures/WALLPAPERS/forest.jpg", "Photo", "Wallpaper")]
    public void when_a_path_has_a_recognised_folder_then_its_classification_is_returned(string remotePath, string expectedLevel1, string expectedLevel2)
        => CategoriseToLevels(remotePath).ShouldBe([(expectedLevel1, expectedLevel2)]);

    [Fact]
    public void when_a_path_has_two_recognised_folders_then_both_classifications_are_returned()
        => CategoriseToLevels("/Pictures/Wallpapers/Screenshots/shot.png").ShouldBe([("Photo", "Wallpaper"), ("Photo", "Screenshot")], ignoreOrder: true);

    [Fact]
    public void when_the_same_folder_name_appears_twice_then_one_classification_is_returned()
        => CategoriseToLevels("/Receipts/2024/Receipts/shop.pdf").ShouldBe([("Document", "Receipt")]);

    [Theory]
    [InlineData("/Documents/report.pdf")]
    [InlineData("/Downloads/setup.exe")]
    [InlineData("/Photos/holiday.jpg")]
    [InlineData("/fedora-docs/guide.md")]
    [InlineData("/Pictures/wallpapers.jpg")]
    [InlineData("")]
    public void when_a_path_has_no_recognised_folder_then_nothing_is_returned(string remotePath)
        => CategoriseToLevels(remotePath).ShouldBeEmpty();
}
