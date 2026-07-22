namespace AStar.Dev.Utilities.Tests.Unit;

public sealed class GivenPathOperationExtensions
{
    [Fact]
    public void when_combine_path_is_called_with_relative_segments_then_returns_the_combined_path()
    {
        string basePath = Path.Join("root", "base");

        string result = basePath.CombinePath("child", "file.txt");

        result.ShouldBe("root/base/child/file.txt");
    }

    [Fact]
    public void when_combine_path_calls_are_chained_without_rooted_segments_then_returns_the_combined_path()
    {
        string result = "base".CombinePath("child2").CombinePath("file.txt");

        result.ShouldBe("base/child2/file.txt");
    }

    [Fact]
    public void when_combine_path_is_called_with_no_segments_then_returns_the_base_path_unchanged() =>
        "root/base".CombinePath().ShouldBe("root/base");

    [Fact]
    public void when_combine_path_is_called_with_a_rooted_segment_then_the_segment_is_joined_rather_than_overriding_the_base_path() =>
        "root/base".CombinePath("/etc/passwd").ShouldBe("root/base/etc/passwd");

    [Fact]
    public void when_combine_path_is_called_with_a_null_entry_in_segments_then_the_null_entry_is_skipped() =>
        "base".CombinePath("child", null!, "file.txt").ShouldBe("base/child/file.txt");

    [Fact]
    public void when_combine_path_is_called_with_a_null_segments_array_then_throws_argument_null_exception()
    {
        string[]? segments = null;

        Action combinePathAction = () => "base".CombinePath(segments!);

        _ = combinePathAction.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void when_clean_path_is_called_with_a_clean_path_then_returns_the_path_unchanged() =>
        "clean/path/file.txt".CleanPath().ShouldBe("clean/path/file.txt");

    [Fact]
    public void when_clean_path_is_called_with_leading_and_trailing_whitespace_then_returns_the_trimmed_path() =>
        "  path/file.txt  ".CleanPath().ShouldBe("path/file.txt");

    [Fact]
    public void when_clean_path_is_called_with_double_quotes_then_replaces_them_with_single_quotes() =>
        "path/\"quoted\"/file.txt".CleanPath().ShouldBe("path/'quoted'/file.txt");

    [Fact]
    public void when_clean_path_is_called_with_a_pipe_character_then_removes_it() =>
        "path/pi|pe/file.txt".CleanPath().ShouldBe("path/pipe/file.txt");

    [Fact]
    public void when_clean_path_is_called_with_the_smoke_character_then_removes_it() =>
        "path/煙smoke/file.txt".CleanPath().ShouldBe("path/smoke/file.txt");

    [Fact]
    public void when_clean_path_is_called_with_non_ascii_characters_then_strips_them() =>
        "path/café/file.txt".CleanPath().ShouldBe("path/caf/file.txt");

    [Fact]
    public void when_clean_path_is_called_with_an_invalid_path_character_then_replaces_it_with_a_space()
    {
        char invalidChar = Path.GetInvalidPathChars().First();
        string path         = $"path{invalidChar}file.txt";

        path.CleanPath().ShouldBe("path file.txt");
    }
}
