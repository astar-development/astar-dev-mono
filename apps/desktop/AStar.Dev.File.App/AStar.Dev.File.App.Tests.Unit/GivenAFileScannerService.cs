using AStar.Dev.File.App.Services;

namespace AStar.Dev.File.App.Tests.Unit;

public class GivenAFileScannerService
{
    [Fact]
    public void when_constructed_with_valid_dependencies_then_instance_is_not_null()
    {
        var dbFactory = Substitute.For<Microsoft.EntityFrameworkCore.IDbContextFactory<Data.FileAppDbContext>>();
        var classifier = Substitute.For<IFileTypeClassifier>();

        var service = new FileScannerService(dbFactory, classifier);
        service.ShouldNotBeNull();
    }

    [Fact]
    public async Task when_scan_is_cancelled_then_operation_canceled_exception_is_thrown()
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
