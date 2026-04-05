using AStar.Dev.OneDrive.Client.Features.FileOperations;
using AStar.Dev.OneDrive.Client.Infrastructure;
using AStar.Dev.OneDrive.Client.Tests.Unit.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.Features.FileOperations;

public sealed class GivenAFileDownloaderImplementation
{
    private const string LocalPath = "/tmp/partial-download.bin";

    [Fact]
    public async Task when_download_is_cancelled_then_partial_file_is_deleted()
    {
        using var graphClient = FakeGraphClients.CreateThrowing(new OperationCanceledException());
        var factory = Substitute.For<IGraphClientFactory>();
        factory.Create(Arg.Any<string>()).Returns(graphClient);

        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(LocalPath, new MockFileData("partial bytes"));

        var sut = new FileDownloader(factory, fileSystem, NullLogger<FileDownloader>.Instance);

        await Should.ThrowAsync<OperationCanceledException>(() =>
            sut.DownloadAsync("token", "remote-id", LocalPath, null, TestContext.Current.CancellationToken));

        fileSystem.File.Exists(LocalPath).ShouldBeFalse();
    }
}
