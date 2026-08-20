using AStar.Dev.File.App.Models;

namespace AStar.Dev.File.App.TestsUnit.Models;

public class GivenAFileType
{
    [Theory]
    [InlineData(FileType.Image)]
    [InlineData(FileType.Document)]
    [InlineData(FileType.Spreadsheet)]
    [InlineData(FileType.Presentation)]
    [InlineData(FileType.Video)]
    [InlineData(FileType.Audio)]
    [InlineData(FileType.Archive)]
    [InlineData(FileType.Code)]
    [InlineData(FileType.Database)]
    [InlineData(FileType.Executable)]
    [InlineData(FileType.Unknown)]
    public void when_checking_a_defined_member_then_it_is_defined(FileType fileType)
        => Enum.IsDefined(fileType).ShouldBeTrue();

    [Fact]
    public void when_counting_members_then_it_has_the_expected_number_of_values()
        => Enum.GetValues<FileType>().Length.ShouldBe(11);
}
