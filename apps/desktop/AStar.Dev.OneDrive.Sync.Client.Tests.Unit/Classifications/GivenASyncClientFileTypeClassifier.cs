using AStar.Dev.OneDrive.Sync.Client.Classifications;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Classifications;

public sealed class GivenASyncClientFileTypeClassifier
{
    private readonly SyncClientFileTypeClassifier sut = new();

    [Theory]
    [InlineData(".jpg")]
    [InlineData(".jpeg")]
    [InlineData(".png")]
    [InlineData(".gif")]
    [InlineData(".bmp")]
    [InlineData(".tiff")]
    [InlineData(".tif")]
    [InlineData(".webp")]
    [InlineData(".svg")]
    [InlineData(".heic")]
    [InlineData(".raw")]
    [InlineData(".ico")]
    [InlineData(".avif")]
    public void when_classifying_image_extension_then_returns_image(string extension) =>
        sut.Classify(extension).ShouldBe(FileType.Image);

    [Theory]
    [InlineData(".pdf")]
    [InlineData(".doc")]
    [InlineData(".docx")]
    [InlineData(".txt")]
    [InlineData(".rtf")]
    [InlineData(".odt")]
    [InlineData(".md")]
    [InlineData(".pages")]
    [InlineData(".epub")]
    public void when_classifying_document_extension_then_returns_document(string extension) =>
        sut.Classify(extension).ShouldBe(FileType.Document);

    [Theory]
    [InlineData(".xls")]
    [InlineData(".xlsx")]
    [InlineData(".csv")]
    [InlineData(".ods")]
    [InlineData(".numbers")]
    public void when_classifying_spreadsheet_extension_then_returns_spreadsheet(string extension) =>
        sut.Classify(extension).ShouldBe(FileType.Spreadsheet);

    [Theory]
    [InlineData(".ppt")]
    [InlineData(".pptx")]
    [InlineData(".odp")]
    [InlineData(".key")]
    public void when_classifying_presentation_extension_then_returns_presentation(string extension) =>
        sut.Classify(extension).ShouldBe(FileType.Presentation);

    [Theory]
    [InlineData(".mp4")]
    [InlineData(".avi")]
    [InlineData(".mov")]
    [InlineData(".mkv")]
    [InlineData(".wmv")]
    [InlineData(".flv")]
    [InlineData(".webm")]
    [InlineData(".m4v")]
    public void when_classifying_video_extension_then_returns_video(string extension) =>
        sut.Classify(extension).ShouldBe(FileType.Video);

    [Theory]
    [InlineData(".mp3")]
    [InlineData(".wav")]
    [InlineData(".flac")]
    [InlineData(".aac")]
    [InlineData(".ogg")]
    [InlineData(".m4a")]
    [InlineData(".wma")]
    public void when_classifying_audio_extension_then_returns_audio(string extension) =>
        sut.Classify(extension).ShouldBe(FileType.Audio);

    [Theory]
    [InlineData(".zip")]
    [InlineData(".rar")]
    [InlineData(".7z")]
    [InlineData(".tar")]
    [InlineData(".gz")]
    [InlineData(".bz2")]
    [InlineData(".xz")]
    public void when_classifying_archive_extension_then_returns_archive(string extension) =>
        sut.Classify(extension).ShouldBe(FileType.Archive);

    [Theory]
    [InlineData(".cs")]
    [InlineData(".py")]
    [InlineData(".js")]
    [InlineData(".ts")]
    [InlineData(".java")]
    [InlineData(".cpp")]
    [InlineData(".c")]
    [InlineData(".h")]
    [InlineData(".go")]
    [InlineData(".rs")]
    [InlineData(".rb")]
    [InlineData(".php")]
    [InlineData(".html")]
    [InlineData(".css")]
    [InlineData(".json")]
    [InlineData(".xml")]
    [InlineData(".yaml")]
    [InlineData(".yml")]
    [InlineData(".sh")]
    [InlineData(".ps1")]
    [InlineData(".sql")]
    public void when_classifying_code_extension_then_returns_code(string extension) =>
        sut.Classify(extension).ShouldBe(FileType.Code);

    [Theory]
    [InlineData(".db")]
    [InlineData(".sqlite")]
    [InlineData(".sqlite3")]
    [InlineData(".mdb")]
    [InlineData(".accdb")]
    public void when_classifying_database_extension_then_returns_database(string extension) =>
        sut.Classify(extension).ShouldBe(FileType.Database);

    [Theory]
    [InlineData(".exe")]
    [InlineData(".dll")]
    [InlineData(".so")]
    [InlineData(".dylib")]
    public void when_classifying_executable_extension_then_returns_executable(string extension) =>
        sut.Classify(extension).ShouldBe(FileType.Executable);

    [Theory]
    [InlineData(".xyz")]
    [InlineData(".unknown")]
    [InlineData(".foo")]
    public void when_classifying_unknown_extension_then_returns_unknown(string extension) =>
        sut.Classify(extension).ShouldBe(FileType.Unknown);

    [Fact]
    public void when_classifying_empty_string_then_returns_unknown() =>
        sut.Classify(string.Empty).ShouldBe(FileType.Unknown);

    [Fact]
    public void when_classifying_null_then_returns_unknown() =>
        sut.Classify(null!).ShouldBe(FileType.Unknown);

    [Theory]
    [InlineData(".JPG")]
    [InlineData(".PNG")]
    [InlineData(".DOCX")]
    public void when_classifying_uppercase_extension_then_returns_correct_type(string extension) =>
        sut.Classify(extension).ShouldNotBe(FileType.Unknown);
}
