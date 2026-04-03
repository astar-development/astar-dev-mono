using AStar.Dev.File.App.Services;

namespace AStar.Dev.File.App.Tests.Unit;

public class FileScannerServiceShould
{
    [Fact]
    public void FileScannerService_CanBeInstantiated()
    {
        var dbFactory = Substitute.For<Microsoft.EntityFrameworkCore.IDbContextFactory<Data.FileAppDbContext>>();
        var classifier = Substitute.For<IFileTypeClassifier>();

        var service = new FileScannerService(dbFactory, classifier);
        service.ShouldNotBeNull();
    }

    [Fact]
    public async Task ScanAsync_WithCancellationRequested_ThrowsOperationCanceledException()
    {
        var dbFactory = Substitute.For<Microsoft.EntityFrameworkCore.IDbContextFactory<Data.FileAppDbContext>>();
        var classifier = Substitute.For<IFileTypeClassifier>();
        var service = new FileScannerService(dbFactory, classifier);

        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var progress = new Progress<ScanProgressUpdate>();
        string tempDir = Path.GetTempPath();

        await Should.ThrowAsync<OperationCanceledException>(
            () => service.ScanAsync(tempDir, progress, cts.Token));
    }
}
