using AStar.Dev.File.App.Data;
using AStar.Dev.File.App.Models;
using AStar.Dev.File.App.Services;
using AStar.Dev.File.App.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.File.App.Tests.Unit;

public class FileViewerServiceShould
{
    private readonly IDbContextFactory<FileAppDbContext> _dbContextFactory;
    private readonly FileViewerService _sut;

    public FileViewerServiceShould()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory<FileAppDbContext>>();
        _sut = new FileViewerService(_dbContextFactory);
    }

    [Fact]
    public async Task ViewFileAsync_WithNullItem_ReturnsWithoutError()
    {
        await _sut.ViewFileAsync(null);
    }

    [Fact]
    public async Task ViewFileAsync_WithValidItem_RaisesFileViewRequestedEvent()
    {
        var scannedFile = CreateScannedFile();
        var displayItem = new ScannedFileDisplayItem(scannedFile);
        bool eventRaised = false;
        ScannedFileDisplayItem? eventItem = null;

        _sut.FileViewRequested += item =>
        {
            eventRaised = true;
            eventItem = item;
        };

        var dbContext = CreateInMemoryDbContext();
        dbContext.ScannedFiles.Add(scannedFile);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        _dbContextFactory
            .CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(dbContext));

        await _sut.ViewFileAsync(displayItem);

        eventRaised.ShouldBeTrue();
        eventItem.ShouldBe(displayItem);
    }

    [Fact]
    public async Task ViewFileAsync_WithNonExistentFile_DoesNotThrow()
    {
        var dbContext = CreateInMemoryDbContext();
        _dbContextFactory
            .CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(dbContext));

        var displayItem = new ScannedFileDisplayItem(new ScannedFile
        {
            Id = 99999,
            RootPath = "/",
            FolderPath = "/nonexistent",
            FileName = "ghost.txt",
            FullPath = "/nonexistent/ghost.txt",
            LastModified = DateTime.UtcNow
        });

        await _sut.ViewFileAsync(displayItem);
    }

    [Fact]
    public async Task FileViewRequested_EventCanHaveMultipleSubscribers()
    {
        var scannedFile = CreateScannedFile();
        var displayItem = new ScannedFileDisplayItem(scannedFile);
        bool firstSubscriberCalled = false;
        bool secondSubscriberCalled = false;

        _sut.FileViewRequested += _ => firstSubscriberCalled = true;
        _sut.FileViewRequested += _ => secondSubscriberCalled = true;

        var dbContext = CreateInMemoryDbContext();
        dbContext.ScannedFiles.Add(scannedFile);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        _dbContextFactory
            .CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(dbContext));

        await _sut.ViewFileAsync(displayItem);

        firstSubscriberCalled.ShouldBeTrue();
        secondSubscriberCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task ViewFileAsync_DbContextFactoryIsCalled()
    {
        var scannedFile = CreateScannedFile();
        var displayItem = new ScannedFileDisplayItem(scannedFile);

        var dbContext = CreateInMemoryDbContext();
        dbContext.ScannedFiles.Add(scannedFile);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        _dbContextFactory
            .CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(dbContext));

        await _sut.ViewFileAsync(displayItem);

        await _dbContextFactory.Received(1).CreateDbContextAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ViewFileAsync_WithValidItem_DbContextIsRequested()
    {
        var dbContext = CreateInMemoryDbContext();
        _dbContextFactory
            .CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(dbContext));

        var scannedFile = CreateScannedFile();
        var displayItem = new ScannedFileDisplayItem(scannedFile);

        await _sut.ViewFileAsync(displayItem);

        await _dbContextFactory.Received(1).CreateDbContextAsync(Arg.Any<CancellationToken>());
    }

    private static FileAppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<FileAppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new FileAppDbContext(options);
    }

    private static ScannedFile CreateScannedFile(
        string fileName = "test.txt",
        FileType fileType = FileType.Unknown) => new()
        {
            RootPath = "/data",
            FolderPath = "/data/docs",
            FileName = fileName,
            FullPath = "/data/docs/" + fileName,
            FileType = fileType,
            LastModified = DateTime.UtcNow,
            SizeInBytes = 1024,
            LastViewed = null,
            PendingDelete = false
        };
}
