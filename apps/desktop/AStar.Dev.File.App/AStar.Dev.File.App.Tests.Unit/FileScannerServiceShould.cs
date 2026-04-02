using AStar.Dev.File.App.Services;

namespace AStar.Dev.File.App.Tests.Unit;

public class FileScannerServiceShould
{
    [Fact]
    public void FileScannerService_CanBeInstantiated()
    {
        var dbFactory = NSubstitute.Substitute.For<Microsoft.EntityFrameworkCore.IDbContextFactory<AStar.Dev.File.App.Data.FileAppDbContext>>();
        var classifier = NSubstitute.Substitute.For<IFileTypeClassifier>();

        var service = new FileScannerService(dbFactory, classifier);
        service.ShouldNotBeNull();
    }

    [Fact]
    public async Task ScanAsync_WithCancellationRequested_ThrowsOperationCanceledException()
    {
        var dbFactory = NSubstitute.Substitute.For<Microsoft.EntityFrameworkCore.IDbContextFactory<AStar.Dev.File.App.Data.FileAppDbContext>>();
        var classifier = NSubstitute.Substitute.For<IFileTypeClassifier>();
        var service = new FileScannerService(dbFactory, classifier);

        var cts = new System.Threading.CancellationTokenSource();
        cts.Cancel();

        var progress = new Progress<ScanProgressUpdate>();
        var tempDir = System.IO.Path.GetTempPath();

        await Shouldly.Should.ThrowAsync<OperationCanceledException>(
            () => service.ScanAsync(tempDir, progress, cts.Token));
    }
}
