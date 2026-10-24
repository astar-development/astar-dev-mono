using AStarDev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Classifications;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Classifications;

public sealed class GivenAFileTypeCategoriser
{
    private readonly FileTypeCategoriser sut = new(new SyncClientFileTypeClassifier());

    private (string Level1, string Level2) CategoriseToLevels(string remotePath)
        => sut.Categorise(remotePath).Match(classification => (classification.Level1, classification.Level2.MapOrDefault(level2 => level2, string.Empty)), () => (string.Empty, string.Empty));

    [Theory]
    [InlineData("/Documents/holiday.jpg", "Photo", "")]
    [InlineData("/Documents/holiday.PNG", "Photo", "")]
    [InlineData("/Documents/report.pdf", "Document", "")]
    [InlineData("/Documents/budget.xlsx", "Document", "Spreadsheet")]
    [InlineData("/Documents/talk.pptx", "Document", "Presentation")]
    [InlineData("/Media/clip.mp4", "Video", "")]
    [InlineData("/Media/song.flac", "Audio", "")]
    [InlineData("/Backups/old.zip", "Archive", "")]
    [InlineData("/Code/Program.cs", "Code", "")]
    [InlineData("/Code/Add-Thing.ps1", "Code", "Script")]
    [InlineData("/Code/build.sh", "Code", "Script")]
    [InlineData("/Data/app.sqlite", "Other", "")]
    [InlineData("/Bin/tool.dll", "Other", "")]
    [InlineData("/Misc/readme", "Other", "")]
    [InlineData("/Misc/strange.qqq", "Other", "")]
    public void when_a_path_is_categorised_then_its_file_type_levels_are_returned(string remotePath, string expectedLevel1, string expectedLevel2)
        => CategoriseToLevels(remotePath).ShouldBe((expectedLevel1, expectedLevel2));

    [Fact]
    public void when_a_path_is_empty_then_none_is_returned()
        => sut.Categorise(string.Empty).Match(_ => false, () => true).ShouldBeTrue();
}
