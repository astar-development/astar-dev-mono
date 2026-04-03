using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Client.Features.FileOperations;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.Features.FileOperations;

public sealed class GivenAFileUploader
{
    private readonly IFileUploader _sut = Substitute.For<IFileUploader>();

    [Fact]
    public async Task when_upload_succeeds_then_returns_file_upload_result()
    {
        var expected = FileUploadResultFactory.Create("remote-item-id", "report.docx", 1024);
        _sut.UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<long>>(), Arg.Any<CancellationToken>())
            .Returns(new Result<FileUploadResult, string>.Ok(expected));

        var result = await _sut.UploadAsync("token", "/local/report.docx", "remote-folder-id", null, TestContext.Current.CancellationToken);

        var ok = result.ShouldBeOfType<Result<FileUploadResult, string>.Ok>();
        ok.Value.RemoteItemId.ShouldBe("remote-item-id");
        ok.Value.FileName.ShouldBe("report.docx");
        ok.Value.BytesUploaded.ShouldBe(1024L);
    }

    [Fact]
    public async Task when_upload_fails_then_returns_error_with_message()
    {
        _sut.UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<long>>(), Arg.Any<CancellationToken>())
            .Returns(new Result<FileUploadResult, string>.Error("Graph API error uploading 'report.docx': Forbidden"));

        var result = await _sut.UploadAsync("token", "/local/report.docx", "remote-folder-id", null, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Result<FileUploadResult, string>.Error>().Reason.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task when_large_file_upload_succeeds_then_returns_bytes_uploaded()
    {
        const long LargeFileSize = 6 * 1024 * 1024L;
        var expected = FileUploadResultFactory.Create("chunked-item-id", "video.mp4", LargeFileSize);
        _sut.UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<long>>(), Arg.Any<CancellationToken>())
            .Returns(new Result<FileUploadResult, string>.Ok(expected));

        var result = await _sut.UploadAsync("token", "/local/video.mp4", "remote-folder-id", null, TestContext.Current.CancellationToken);

        var ok = result.ShouldBeOfType<Result<FileUploadResult, string>.Ok>();
        ok.Value.BytesUploaded.ShouldBe(LargeFileSize);
    }

    [Fact]
    public async Task when_small_file_upload_succeeds_then_returns_bytes_uploaded()
    {
        const long SmallFileSize = 512 * 1024L;
        var expected = FileUploadResultFactory.Create("direct-item-id", "notes.txt", SmallFileSize);
        _sut.UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<long>>(), Arg.Any<CancellationToken>())
            .Returns(new Result<FileUploadResult, string>.Ok(expected));

        var result = await _sut.UploadAsync("token", "/local/notes.txt", "remote-folder-id", null, TestContext.Current.CancellationToken);

        var ok = result.ShouldBeOfType<Result<FileUploadResult, string>.Ok>();
        ok.Value.BytesUploaded.ShouldBe(SmallFileSize);
    }

    [Fact]
    public async Task when_upload_progress_is_reported_then_progress_handler_is_called()
    {
        const long FileSize = 1024L;
        var progress = Substitute.For<IProgress<long>>();
        var expected = FileUploadResultFactory.Create("item-id", "file.txt", FileSize);
        _sut.UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<long>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callInfo.Arg<IProgress<long>>()?.Report(FileSize);

                return Task.FromResult<Result<FileUploadResult, string>>(new Result<FileUploadResult, string>.Ok(expected));
            });

        await _sut.UploadAsync("token", "/local/file.txt", "folder-id", progress, TestContext.Current.CancellationToken);

        progress.Received(1).Report(FileSize);
    }
}
