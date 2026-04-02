using AStar.Dev.File.App.Models;
using AStar.Dev.File.App.Services;

namespace AStar.Dev.File.App.Tests.Unit;

public class FileTypeClassifierTests
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
    public void Classify_ImageExtensions_ReturnsImage(string ext, FileType expected)
        => _sut.Classify(ext).ShouldBe(expected);

    [Theory]
    [InlineData(".pdf", FileType.Document)]
    [InlineData(".doc", FileType.Document)]
    [InlineData(".docx", FileType.Document)]
    [InlineData(".txt", FileType.Document)]
    [InlineData(".md", FileType.Document)]
    [InlineData(".epub", FileType.Document)]
    public void Classify_DocumentExtensions_ReturnsDocument(string ext, FileType expected)
        => _sut.Classify(ext).ShouldBe(expected);

    [Theory]
    [InlineData(".xls", FileType.Spreadsheet)]
    [InlineData(".xlsx", FileType.Spreadsheet)]
    [InlineData(".csv", FileType.Spreadsheet)]
    [InlineData(".ods", FileType.Spreadsheet)]
    public void Classify_SpreadsheetExtensions_ReturnsSpreadsheet(string ext, FileType expected)
        => _sut.Classify(ext).ShouldBe(expected);

    [Theory]
    [InlineData(".ppt", FileType.Presentation)]
    [InlineData(".pptx", FileType.Presentation)]
    [InlineData(".key", FileType.Presentation)]
    public void Classify_PresentationExtensions_ReturnsPresentation(string ext, FileType expected)
        => _sut.Classify(ext).ShouldBe(expected);

    [Theory]
    [InlineData(".mp4", FileType.Video)]
    [InlineData(".avi", FileType.Video)]
    [InlineData(".mov", FileType.Video)]
    [InlineData(".mkv", FileType.Video)]
    public void Classify_VideoExtensions_ReturnsVideo(string ext, FileType expected)
        => _sut.Classify(ext).ShouldBe(expected);

    [Theory]
    [InlineData(".mp3", FileType.Audio)]
    [InlineData(".wav", FileType.Audio)]
    [InlineData(".flac", FileType.Audio)]
    public void Classify_AudioExtensions_ReturnsAudio(string ext, FileType expected)
        => _sut.Classify(ext).ShouldBe(expected);

    [Theory]
    [InlineData(".zip", FileType.Archive)]
    [InlineData(".rar", FileType.Archive)]
    [InlineData(".7z", FileType.Archive)]
    [InlineData(".tar", FileType.Archive)]
    public void Classify_ArchiveExtensions_ReturnsArchive(string ext, FileType expected)
        => _sut.Classify(ext).ShouldBe(expected);

    [Theory]
    [InlineData(".cs", FileType.Code)]
    [InlineData(".py", FileType.Code)]
    [InlineData(".js", FileType.Code)]
    [InlineData(".ts", FileType.Code)]
    [InlineData(".json", FileType.Code)]
    [InlineData(".sql", FileType.Code)]
    public void Classify_CodeExtensions_ReturnsCode(string ext, FileType expected)
        => _sut.Classify(ext).ShouldBe(expected);

    [Theory]
    [InlineData(".db", FileType.Database)]
    [InlineData(".sqlite", FileType.Database)]
    [InlineData(".sqlite3", FileType.Database)]
    public void Classify_DatabaseExtensions_ReturnsDatabase(string ext, FileType expected)
        => _sut.Classify(ext).ShouldBe(expected);

    [Theory]
    [InlineData(".exe", FileType.Executable)]
    [InlineData(".dll", FileType.Executable)]
    [InlineData(".so", FileType.Executable)]
    public void Classify_ExecutableExtensions_ReturnsExecutable(string ext, FileType expected)
        => _sut.Classify(ext).ShouldBe(expected);

    [Theory]
    [InlineData(".xyz")]
    [InlineData(".foobar")]
    [InlineData(".123")]
    public void Classify_UnknownExtension_ReturnsUnknown(string ext)
        => _sut.Classify(ext).ShouldBe(FileType.Unknown);

    [Fact]
    public void Classify_EmptyString_ReturnsUnknown()
        => _sut.Classify(string.Empty).ShouldBe(FileType.Unknown);
}
