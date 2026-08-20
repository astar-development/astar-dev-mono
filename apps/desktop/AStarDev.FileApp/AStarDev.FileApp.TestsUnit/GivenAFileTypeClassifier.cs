using AStar.Dev.File.App.Models;
using AStar.Dev.File.App.Services;

namespace AStar.Dev.File.App.TestsUnit;

public class GivenAFileTypeClassifier
{
    private readonly FileTypeClassifier _sut = new();

    [Theory]
    [InlineData(".jpg", FileType.Image)]
    [InlineData(".JPG", FileType.Image)]
    [InlineData(".jpeg", FileType.Image)]
    [InlineData(".png", FileType.Image)]
    [InlineData(".gif", FileType.Image)]
    [InlineData(".bmp", FileType.Image)]
    [InlineData(".webp", FileType.Image)]
    [InlineData(".svg", FileType.Image)]
    [InlineData(".heic", FileType.Image)]
    [InlineData(".avif", FileType.Image)]
    public void when_classifying_image_extensions_then_returns_image(string ext, FileType expected)
        => _sut.Classify(ext).ShouldBe(expected);

    [Theory]
    [InlineData(".pdf", FileType.Document)]
    [InlineData(".doc", FileType.Document)]
    [InlineData(".docx", FileType.Document)]
    [InlineData(".txt", FileType.Document)]
    [InlineData(".md", FileType.Document)]
    [InlineData(".epub", FileType.Document)]
    public void when_classifying_document_extensions_then_returns_document(string ext, FileType expected)
        => _sut.Classify(ext).ShouldBe(expected);

    [Theory]
    [InlineData(".xls", FileType.Spreadsheet)]
    [InlineData(".xlsx", FileType.Spreadsheet)]
    [InlineData(".csv", FileType.Spreadsheet)]
    [InlineData(".ods", FileType.Spreadsheet)]
    public void when_classifying_spreadsheet_extensions_then_returns_spreadsheet(string ext, FileType expected)
        => _sut.Classify(ext).ShouldBe(expected);

    [Theory]
    [InlineData(".ppt", FileType.Presentation)]
    [InlineData(".pptx", FileType.Presentation)]
    [InlineData(".key", FileType.Presentation)]
    public void when_classifying_presentation_extensions_then_returns_presentation(string ext, FileType expected)
        => _sut.Classify(ext).ShouldBe(expected);

    [Theory]
    [InlineData(".mp4", FileType.Video)]
    [InlineData(".avi", FileType.Video)]
    [InlineData(".mov", FileType.Video)]
    [InlineData(".mkv", FileType.Video)]
    public void when_classifying_video_extensions_then_returns_video(string ext, FileType expected)
        => _sut.Classify(ext).ShouldBe(expected);

    [Theory]
    [InlineData(".mp3", FileType.Audio)]
    [InlineData(".wav", FileType.Audio)]
    [InlineData(".flac", FileType.Audio)]
    public void when_classifying_audio_extensions_then_returns_audio(string ext, FileType expected)
        => _sut.Classify(ext).ShouldBe(expected);

    [Theory]
    [InlineData(".zip", FileType.Archive)]
    [InlineData(".rar", FileType.Archive)]
    [InlineData(".7z", FileType.Archive)]
    [InlineData(".tar", FileType.Archive)]
    public void when_classifying_archive_extensions_then_returns_archive(string ext, FileType expected)
        => _sut.Classify(ext).ShouldBe(expected);

    [Theory]
    [InlineData(".cs", FileType.Code)]
    [InlineData(".py", FileType.Code)]
    [InlineData(".js", FileType.Code)]
    [InlineData(".ts", FileType.Code)]
    [InlineData(".json", FileType.Code)]
    [InlineData(".sql", FileType.Code)]
    public void when_classifying_code_extensions_then_returns_code(string ext, FileType expected)
        => _sut.Classify(ext).ShouldBe(expected);

    [Theory]
    [InlineData(".db", FileType.Database)]
    [InlineData(".sqlite", FileType.Database)]
    [InlineData(".sqlite3", FileType.Database)]
    public void when_classifying_database_extensions_then_returns_database(string ext, FileType expected)
        => _sut.Classify(ext).ShouldBe(expected);

    [Theory]
    [InlineData(".exe", FileType.Executable)]
    [InlineData(".dll", FileType.Executable)]
    [InlineData(".so", FileType.Executable)]
    public void when_classifying_executable_extensions_then_returns_executable(string ext, FileType expected)
        => _sut.Classify(ext).ShouldBe(expected);

    [Theory]
    [InlineData(".xyz")]
    [InlineData(".foobar")]
    [InlineData(".123")]
    public void when_classifying_an_unknown_extension_then_returns_unknown(string ext)
        => _sut.Classify(ext).ShouldBe(FileType.Unknown);

    [Fact]
    public void when_classifying_an_empty_string_then_returns_unknown()
        => _sut.Classify(string.Empty).ShouldBe(FileType.Unknown);
}
