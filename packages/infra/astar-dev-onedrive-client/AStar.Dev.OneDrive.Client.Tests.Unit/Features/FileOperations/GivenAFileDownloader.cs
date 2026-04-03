using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Client.Features.FileOperations;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.Features.FileOperations;

public sealed class GivenAFileDownloader
{
    private readonly IFileDownloader _sut = Substitute.For<IFileDownloader>();

    [Fact]
    public async Task when_download_succeeds_then_returns_file_download_result()
    {
        var expected = FileDownloadResultFactory.Create("/local/docs/report.docx", 1024);
        _sut.DownloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<long>>(), Arg.Any<CancellationToken>())
            .Returns(new Result<FileDownloadResult, string>.Ok(expected));

        var result = await _sut.DownloadAsync("token", "remote-id", "/local/docs/report.docx", null, TestContext.Current.CancellationToken);

        var ok = result.ShouldBeOfType<Result<FileDownloadResult, string>.Ok>();
        ok.Value.LocalPath.ShouldBe("/local/docs/report.docx");
        ok.Value.BytesWritten.ShouldBe(1024L);
    }

    [Fact]
    public async Task when_download_fails_then_returns_error_with_message()
    {
        _sut.DownloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<long>>(), Arg.Any<CancellationToken>())
            .Returns(new Result<FileDownloadResult, string>.Error("Graph API error downloading 'remote-id': Not Found"));

        var result = await _sut.DownloadAsync("token", "remote-id", "/local/docs/report.docx", null, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Result<FileDownloadResult, string>.Error>().Reason.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task when_download_completes_then_progress_handler_is_called()
    {
        const long FileSize = 2048L;
        var progress = Substitute.For<IProgress<long>>();
        var expected = FileDownloadResultFactory.Create("/tmp/file.bin", FileSize);
        _sut.DownloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<long>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callInfo.Arg<IProgress<long>>()?.Report(FileSize);

                return Task.FromResult<Result<FileDownloadResult, string>>(new Result<FileDownloadResult, string>.Ok(expected));
            });

        await _sut.DownloadAsync("token", "remote-id", "/tmp/file.bin", progress, TestContext.Current.CancellationToken);

        progress.Received(1).Report(FileSize);
    }

    [Fact]
    public async Task when_download_is_called_with_empty_access_token_then_returns_error()
    {
        _sut.DownloadAsync(string.Empty, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<long>>(), Arg.Any<CancellationToken>())
            .Returns(new Result<FileDownloadResult, string>.Error("Access token must not be empty."));

        var result = await _sut.DownloadAsync(string.Empty, "remote-id", "/tmp/file.bin", null, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Result<FileDownloadResult, string>.Error>().Reason.ShouldNotBeNullOrWhiteSpace();
    }
}
