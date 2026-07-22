using System.Text.Json;

namespace AStar.Dev.Utilities.Tests.Unit;

public sealed class GivenStringExtensions
{
    private const string AnyJson = "{\"AnyInt\":0,\"AnyString\":\"\"}";
    private const string NotNullString = "value does not matter";
    private const string WhitespaceString = " ";
#pragma warning disable CA1805
    private readonly string? nullString = null;
#pragma warning restore CA1805

    private static string? MaybeNull(bool returnNull) =>
        returnNull ? null : NotNullString;

    [Fact]
    public void when_is_null_is_called_then_returns_the_expected_result() =>
        nullString.IsNull().ShouldBeTrue();

    [Fact]
    public void when_is_null_is_called_with_a_non_null_string_then_returns_false() =>
        NotNullString.IsNull().ShouldBeFalse();

    [Fact]
    public void when_is_null_returns_false_then_the_compiler_narrows_the_value_to_not_null()
    {
        string? value = MaybeNull(false);

        if (!value.IsNull())
            value.Length.ShouldBe(NotNullString.Length);
    }

    [Fact]
    public void when_is_not_null_is_called_then_returns_the_expected_result() =>
        NotNullString.IsNotNull().ShouldBeTrue();

    [Fact]
    public void when_is_not_null_is_called_with_a_null_string_then_returns_false() =>
        nullString.IsNotNull().ShouldBeFalse();

    [Fact]
    public void when_is_not_null_returns_true_then_the_compiler_narrows_the_value_to_not_null()
    {
        string? value = MaybeNull(false);

        if (value.IsNotNull())
            value.Length.ShouldBe(NotNullString.Length);
    }

    [Fact]
    public void when_is_null_or_white_space_is_called_then_returns_the_expected_result() =>
        WhitespaceString.IsNullOrWhiteSpace().ShouldBeTrue();

    [Fact]
    public void when_is_null_or_white_space_is_called_with_a_non_null_string_then_returns_false() =>
        NotNullString.IsNullOrWhiteSpace().ShouldBeFalse();

    [Fact]
    public void when_is_null_or_white_space_returns_false_then_the_compiler_narrows_the_value_to_not_null()
    {
        string? value = MaybeNull(false);

        if (!value.IsNullOrWhiteSpace())
            value.Length.ShouldBe(NotNullString.Length);
    }

    [Fact]
    public void when_is_not_null_or_white_space_is_called_then_returns_the_expected_result() =>
        NotNullString.IsNotNullOrWhiteSpace().ShouldBeTrue();

    [Fact]
    public void when_is_not_null_or_white_space_returns_true_then_the_compiler_narrows_the_value_to_not_null()
    {
        string? value = MaybeNull(false);

        if (value.IsNotNullOrWhiteSpace())
            value.Length.ShouldBe(NotNullString.Length);
    }

    [Fact]
    public void when_is_not_null_or_white_space_is_called_with_a_null_string_then_returns_false() =>
        nullString.IsNotNullOrWhiteSpace().ShouldBeFalse();

    [Fact]
    public void when_is_not_null_or_white_space_is_called_with_a_whitespace_string_then_returns_false() =>
        WhitespaceString.IsNotNullOrWhiteSpace().ShouldBeFalse();

    [Fact]
    public void when_from_json_is_called_then_returns_the_expected_result() =>
        AnyJson.FromJson<AnyClass>().ShouldBeEquivalentTo(new AnyClass());

    [Fact]
    public void when_from_json_is_called_with_json_serializer_options_then_returns_the_expected_result() =>
        AnyJson.FromJson<AnyClass>(new()).ShouldBeEquivalentTo(new AnyClass());

    [Fact]
    public void when_from_json_is_called_with_invalid_json_then_throws_json_exception()
    {
        Action fromJsonAction = () => "not-valid-json".FromJson<AnyClass>();

        _ = fromJsonAction.ShouldThrow<JsonException>();
    }

    [Theory]
    [InlineData("no-Extension", false)]
    [InlineData("Wrong-Extension.txt", false)]
    [InlineData("Wrong-Extension.DOC", false)]
    [InlineData("Wrong-Extension.PdF", false)]
    [InlineData("Correct-Extension.jpG", true)]
    [InlineData("Correct-Extension.jpeG", true)]
    [InlineData("Correct-Extension.bmp", true)]
    [InlineData("Write-Extension.png", true)]
    [InlineData("Correct-Extension.gif", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void when_is_image_is_called_then_returns_the_expected_results(string? fileName, bool expectedResponse) =>
        fileName!.IsImage().ShouldBe(expectedResponse);

    [Theory]
    [InlineData("no-Truncation", 20, "no-Truncation")]
    [InlineData("Small-String-Truncation.txt", 10, "Small-Stri")]
    [InlineData("Small-String-Truncation.DOC", 15, "Small-String-Tr")]
    [InlineData("Small-String-Truncation.PdF", 20, "Small-String-Truncat")]
    [InlineData("Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String--Truncation.jpG", 100,
                "Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-Str")]
    [InlineData("Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String--Truncation.jpeG", 120,
                "Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Lar")]
    [InlineData("Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String--Truncation.bmp", 140,
                "Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-Stri")]
    [InlineData("Write-Truncation.png", 10, "Write-Trun")]
    [InlineData("Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String--Truncation.gif", 160,
                "Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String--Tru")]
    [InlineData("no-Truncation-when-zero", 0, "no-Truncation-when-zero")]
    [InlineData("no-Truncation-when-negative", -1, "no-Truncation-when-negative")]
    [InlineData("", 5, "")]
    public void when_truncate_if_required_is_called_then_returns_the_expected_results(string fileName, int truncateLength, string expectedResponse) =>
        fileName.TruncateIfRequired(truncateLength).ShouldBe(expectedResponse);

    [Theory]
    [InlineData("no-number", false)]
    [InlineData("number-at-the-end-123", false)]
    [InlineData("123-number-at-the-beginning", false)]
    [InlineData("number-in-the-123-middle", false)]
    [InlineData("1", true)]
    [InlineData("12", true)]
    [InlineData("123456", true)]
    [InlineData("", true)]
    [InlineData("1_2.3", true)]
    [InlineData("1.2_3", true)]
    public void when_is_number_only_is_called_then_returns_the_expected_results(string fileName, bool expectedResponse) =>
        fileName.IsNumberOnly().ShouldBe(expectedResponse);


    [Theory]
    [InlineData("path/to-some_file.txt", "path to some file.txt")]
    [InlineData("folder/sub-folder/file_name.txt", "folder sub folder file name.txt")]
    [InlineData("already clean.txt", "already clean.txt")]
    [InlineData("-_-", "   ")]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    public void when_sanitize_file_path_is_called_then_returns_the_expected_result(string? input, string expected) => input!.SanitizeFilePath().ShouldBe(expected);

    [Theory]
    [InlineData("file.txt", ".txt", "file")]
    [InlineData("file.TXT", ".txt", "file")]
    [InlineData("file.txt", ".doc", "file.txt")]
    [InlineData("", ".txt", "")]
    [InlineData("file.txt", "", "file.txt")]
    public void when_remove_trailing_is_called_then_returns_the_expected_result(string value, string removeTrailing, string expected) =>
        value.RemoveTrailing(removeTrailing).ShouldBe(expected);

    [Theory]
    [InlineData("file", ".txt", "file.txt")]
    [InlineData("file.txt", ".txt", "file.txt")]
    [InlineData("file.TXT", ".txt", "file.TXT")]
    [InlineData("", ".txt", "")]
    [InlineData("file", "", "file")]
    public void when_ensure_trailing_is_called_then_returns_the_expected_result(string value, string ensureTrailing, string expected) =>
        value.EnsureTrailing(ensureTrailing).ShouldBe(expected);

    [Fact]
    public void when_ensure_trailing_is_called_on_a_uri_without_the_suffix_then_appends_it() =>
        new Uri("https://example.com/path").EnsureTrailing("/").ShouldBe("https://example.com/path/");

    [Fact]
    public void when_ensure_trailing_is_called_on_a_uri_with_the_suffix_already_present_then_returns_it_unchanged() =>
        new Uri("https://example.com/path/").EnsureTrailing("/").ShouldBe("https://example.com/path/");

    [Fact]
    public void when_ensure_trailing_slash_is_called_on_a_uri_without_a_trailing_slash_then_appends_it() =>
        new Uri("https://example.com/path").EnsureTrailingSlash().ShouldBe("https://example.com/path/");

    [Fact]
    public void when_ensure_trailing_slash_is_called_on_a_uri_with_a_trailing_slash_then_returns_it_unchanged() =>
        new Uri("https://example.com/path/").EnsureTrailingSlash().ShouldBe("https://example.com/path/");

    [Theory]
    [InlineData(@"folder\sub\file.txt", "/folder/sub/file.txt")]
    [InlineData("folder/sub/file.txt", "/folder/sub/file.txt")]
    [InlineData("/already/prefixed/", "/already/prefixed")]
    [InlineData("", "/")]
    [InlineData("   ", "/")]
    public void when_normalize_linux_is_called_then_returns_the_expected_result(string path, string expected) =>
        path.NormalizeLinux().ShouldBe(expected);

    [Theory]
    [InlineData("folder/sub/file.txt", @"\folder\sub\file.txt")]
    [InlineData(@"folder\sub\file.txt", @"\folder\sub\file.txt")]
    [InlineData(@"\already\prefixed\", @"\already\prefixed")]
    [InlineData("", @"\")]
    [InlineData("   ", @"\")]
    public void when_normalize_windows_is_called_then_returns_the_expected_result(string path, string expected) =>
        path.NormalizeWindows().ShouldBe(expected);

    [Theory]
    [InlineData(1, "1 B")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1024 * 1024, "1.0 MB")]
    [InlineData(1024 * 1024 * 1024, "1.0 GB")]
    [InlineData(0, "")]
    [InlineData(512, "512 B")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(5 * 1024 * 1024, "5.0 MB")]
    [InlineData((5 * 1024 * 1024) + (512 * 1024 * 1024), "517.0 MB")]
    public void when_file_size_to_text_is_called_then_returns_the_human_readable_format(long fileSizeToConvert, string expected) => fileSizeToConvert.FileSizeToText().ShouldBe(expected);

    [Theory]
    [InlineData("example text", "Example Text")]
    [InlineData("exampletext", "Exampletext")]
    [InlineData("EXAMPLE TEXT", "Example Text")]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void when_to_title_case_is_called_then_returns_the_expected_result(string? input, string expected) => input!.ToTitleCase().ShouldBe(expected);
}
