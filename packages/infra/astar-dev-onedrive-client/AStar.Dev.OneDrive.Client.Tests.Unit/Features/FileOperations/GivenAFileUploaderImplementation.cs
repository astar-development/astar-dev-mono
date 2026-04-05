using AStar.Dev.OneDrive.Client.Features.FileOperations;
using AStar.Dev.OneDrive.Client.Infrastructure;
using AStar.Dev.OneDrive.Client.Tests.Unit.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.Features.FileOperations;

public sealed class GivenAFileUploaderImplementation
{
    private const long FourMb = 4 * 1024 * 1024;

    [Fact]
    public async Task when_file_is_smaller_than_4mb_then_direct_upload_path_is_used()
    {
        var (graphClient, captureHandler) = FakeGraphClients.CreateCapturing();
        using var _ = graphClient;
        var factory = Substitute.For<IGraphClientFactory>();
        factory.Create(Arg.Any<string>()).Returns(graphClient);

        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/tmp/small.txt", new MockFileData(new byte[512 * 1024]));

        var sut = new FileUploader(factory, fileSystem, Substitute.For<IHttpClientFactory>(), NullLogger<FileUploader>.Instance);

        await sut.UploadAsync("token", "/tmp/small.txt", "folder-id", null, TestContext.Current.CancellationToken);

        captureHandler.CapturedPaths.ShouldContain(p => p.EndsWith("/content", StringComparison.OrdinalIgnoreCase));
        captureHandler.CapturedPaths.ShouldNotContain(p => p.Contains("createUploadSession", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task when_file_is_larger_than_4mb_then_chunked_upload_path_is_used()
    {
        var (graphClient, captureHandler) = FakeGraphClients.CreateCapturing();
        using var _ = graphClient;
        var factory = Substitute.For<IGraphClientFactory>();
        factory.Create(Arg.Any<string>()).Returns(graphClient);

        var fileSystem = new MockFileSystem();
        fileSystem.AddFile("/tmp/large.bin", new MockFileData(new byte[FourMb + 1]));

        var sut = new FileUploader(factory, fileSystem, Substitute.For<IHttpClientFactory>(), NullLogger<FileUploader>.Instance);

        await sut.UploadAsync("token", "/tmp/large.bin", "folder-id", null, TestContext.Current.CancellationToken);

        captureHandler.CapturedPaths.ShouldContain(p => p.Contains("createUploadSession", StringComparison.OrdinalIgnoreCase));
        captureHandler.CapturedPaths.ShouldNotContain(p => p.EndsWith("/content", StringComparison.OrdinalIgnoreCase));
    }
}
