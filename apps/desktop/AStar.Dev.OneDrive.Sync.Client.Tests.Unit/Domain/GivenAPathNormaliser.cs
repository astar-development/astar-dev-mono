using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

public sealed class GivenAPathNormaliser
{
    private const string RootedEightSegmentPath = "/a/b/c/d/e/f/g/meaningful/further/file.jpg";
    private const string RootedExactlySevenSegments = "/a/b/c/d/e/f/g";

    private static readonly string[] MeaningfulAndFurther = ["meaningful", "further"];

    [Fact]
    public void when_strip_root_path_called_with_eight_segment_rooted_path_then_meaningful_suffix_is_returned() =>
        PathNormaliser.StripRootPath(RootedEightSegmentPath).ShouldBe("meaningful/further/file.jpg");

    [Fact]
    public void when_strip_root_path_called_with_path_that_has_exactly_seven_non_root_segments_then_empty_string_is_returned() =>
        PathNormaliser.StripRootPath(RootedExactlySevenSegments).ShouldBe(string.Empty);

    [Fact]
    public void when_strip_root_path_called_with_path_that_has_fewer_than_eight_segments_then_empty_string_is_returned() =>
        PathNormaliser.StripRootPath("/a/b/c").ShouldBe(string.Empty);

    [Fact]
    public void when_strip_root_path_called_with_empty_string_then_empty_string_is_returned() =>
        PathNormaliser.StripRootPath(string.Empty).ShouldBe(string.Empty);

    [Fact]
    public void when_strip_root_path_called_with_leading_slash_then_leading_slash_does_not_count_as_a_segment()
    {
        var result = PathNormaliser.StripRootPath(RootedEightSegmentPath);

        result.ShouldBe("meaningful/further/file.jpg");
    }

    [Fact]
    public void when_strip_root_path_called_with_path_without_leading_slash_then_meaningful_suffix_is_returned() =>
        PathNormaliser.StripRootPath("a/b/c/d/e/f/g/meaningful/further/file.jpg").ShouldBe("meaningful/further/file.jpg");

    [Fact]
    public void when_get_folder_segments_called_with_valid_stripped_path_then_all_folder_names_are_returned()
    {
        var segments = PathNormaliser.GetFolderSegments("meaningful/further/file.jpg");

        segments.ShouldBe(MeaningfulAndFurther);
    }

    [Fact]
    public void when_get_folder_segments_called_with_filename_only_then_empty_list_is_returned()
    {
        var segments = PathNormaliser.GetFolderSegments("file.jpg");

        segments.ShouldBeEmpty();
    }

    [Fact]
    public void when_get_folder_segments_called_with_empty_string_then_empty_list_is_returned()
    {
        var segments = PathNormaliser.GetFolderSegments(string.Empty);

        segments.ShouldBeEmpty();
    }

    [Fact]
    public void when_get_folder_segments_called_with_leading_slash_then_leading_slash_does_not_produce_empty_segment()
    {
        var segments = PathNormaliser.GetFolderSegments("/meaningful/further/file.jpg");

        segments.ShouldBe(MeaningfulAndFurther);
    }

    [Fact]
    public void when_get_filename_stem_called_with_valid_stripped_path_then_filename_without_extension_is_returned() =>
        PathNormaliser.GetFilenameStem("meaningful/further/file.jpg").ShouldBe("file");

    [Fact]
    public void when_get_filename_stem_called_with_filename_only_then_stem_without_extension_is_returned() =>
        PathNormaliser.GetFilenameStem("document.pdf").ShouldBe("document");

    [Fact]
    public void when_get_filename_stem_called_with_filename_without_extension_then_full_name_is_returned() =>
        PathNormaliser.GetFilenameStem("meaningful/further/file").ShouldBe("file");

    [Fact]
    public void when_get_filename_stem_called_with_empty_string_then_empty_string_is_returned() =>
        PathNormaliser.GetFilenameStem(string.Empty).ShouldBe(string.Empty);

    [Fact]
    public void when_get_filename_stem_called_with_path_with_leading_slash_then_stem_is_returned_correctly() =>
        PathNormaliser.GetFilenameStem("/meaningful/further/file.jpg").ShouldBe("file");

    [Fact]
    public void when_get_folder_segments_return_type_is_readonly_list()
    {
        var segments = PathNormaliser.GetFolderSegments("a/b/c.jpg");

        _ = segments.ShouldBeAssignableTo<IReadOnlyList<string>>();
    }
}
