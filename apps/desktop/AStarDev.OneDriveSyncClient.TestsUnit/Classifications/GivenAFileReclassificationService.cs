using AStarDev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStarDev.OneDriveSyncClient.Classifications;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Classifications;

public sealed class GivenAFileReclassificationService : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly AppDbContext seedingContext;
    private readonly FileReclassificationService sut;
    private int fileCounter;
    private bool disposed;

    public GivenAFileReclassificationService()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        seedingContext = new AppDbContext(options);
        _ = seedingContext.Database.EnsureCreated();
        seedingContext.Accounts.Add(new AccountEntity { Id = new AccountId("account-1"), Profile = AccountProfileFactory.Create("Test User", "test@test.com") });
        seedingContext.SaveChanges();
        var factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(new AppDbContext(options)));
        var repository = new FileClassificationRepository(factory, NullLogger<FileClassificationRepository>.Instance);
        sut = new FileReclassificationService(factory, repository, new RuleBasedFileAutoCategorisor(), new FileTypeCategoriser(new SyncClientFileTypeClassifier()), new FolderNameCategoriser(), new CategoryResolutionService(factory));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposed)
            return;

        disposed = true;

        if (!disposing)
            return;

        seedingContext.Dispose();
        connection.Dispose();
    }

    private async Task<FileId> AddFileAsync(string remotePath, bool isFolder = false, bool withFileDetail = true)
    {
        fileCounter++;
        FileId? fileDetailId = null;

        if (withFileDetail)
        {
            var fileDetail = new FileDetailEntity
            {
                FileName = new FileName($"file-{fileCounter}.bin"),
                DirectoryName = new DirectoryName("/local"),
                FileHandle = new FileHandle($"handle-{fileCounter}"),
                FileAccessDetail = new FileAccessDetailEntity(),
                ImageDetail = new ImageDetailEntity(),
                DeletionStatus = new DeletionStatusEntity()
            };
            seedingContext.Files.Add(fileDetail);
            await seedingContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            fileDetailId = fileDetail.Id;
        }

        seedingContext.SyncedItems.Add(new SyncedItemEntity { AccountId = new AccountId("account-1"), RemoteItemId = new OneDriveItemId($"item-{fileCounter}"), RemotePath = remotePath, LocalPath = $"/local/file-{fileCounter}.bin", IsFolder = isFolder, FileDetailId = fileDetailId });
        await seedingContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return fileDetailId ?? default;
    }

    private async Task<int> AddManualClassificationAsync(FileId fileDetailId, string categoryName)
    {
        var category = new FileClassificationCategoryEntity { Name = categoryName, Level = 1 };
        seedingContext.FileClassificationCategories.Add(category);
        await seedingContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        seedingContext.FileClassifications.Add(new FileClassificationEntity { FileDetailId = fileDetailId, CategoryId = category.Id });
        await seedingContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return category.Id;
    }

    private int ClassificationCount(FileId fileDetailId) => seedingContext.FileClassifications.AsNoTracking().Count(link => link.FileDetailId == fileDetailId);

    private static ReclassificationSummary Unwrap(Result<ReclassificationSummary, string> result) => result.ShouldBeOfType<Ok<ReclassificationSummary, string>>().Value;

    [Fact]
    public async Task when_a_file_has_no_classifications_then_classifications_are_added()
    {
        var fileDetailId = await AddFileAsync("/Pictures/holiday.jpg");

        await sut.ReclassifyAllAsync(new Progress<ReclassificationProgress>(), TestContext.Current.CancellationToken);

        ClassificationCount(fileDetailId).ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task when_a_file_has_a_manual_classification_then_it_is_kept_and_new_ones_are_added()
    {
        var fileDetailId = await AddFileAsync("/Pictures/holiday.jpg");
        int manualCategoryId = await AddManualClassificationAsync(fileDetailId, "My Own Category");

        await sut.ReclassifyAllAsync(new Progress<ReclassificationProgress>(), TestContext.Current.CancellationToken);

        seedingContext.FileClassifications.AsNoTracking().Any(link => link.FileDetailId == fileDetailId && link.CategoryId == manualCategoryId).ShouldBeTrue();
        ClassificationCount(fileDetailId).ShouldBeGreaterThan(1);
    }

    [Fact]
    public async Task when_run_twice_then_the_second_run_adds_nothing()
    {
        var fileDetailId = await AddFileAsync("/Pictures/holiday.jpg");
        await sut.ReclassifyAllAsync(new Progress<ReclassificationProgress>(), TestContext.Current.CancellationToken);
        int countAfterFirstRun = ClassificationCount(fileDetailId);

        var secondRun = Unwrap(await sut.ReclassifyAllAsync(new Progress<ReclassificationProgress>(), TestContext.Current.CancellationToken));

        secondRun.ClassificationsAdded.ShouldBe(0);
        ClassificationCount(fileDetailId).ShouldBe(countAfterFirstRun);
    }

    [Fact]
    public async Task when_the_database_holds_folders_and_unlinked_items_then_only_files_are_processed()
    {
        await AddFileAsync("/Pictures/holiday.jpg");
        await AddFileAsync("/Pictures", isFolder: true, withFileDetail: false);
        await AddFileAsync("/Pictures/orphan.jpg", withFileDetail: false);

        var summary = Unwrap(await sut.ReclassifyAllAsync(new Progress<ReclassificationProgress>(), TestContext.Current.CancellationToken));

        summary.FilesProcessed.ShouldBe(1);
    }

    [Fact]
    public async Task when_files_are_processed_then_progress_is_reported_for_each_file()
    {
        await AddFileAsync("/Pictures/one.jpg");
        await AddFileAsync("/Pictures/two.jpg");
        List<ReclassificationProgress> reported = [];
        var progress = new SynchronousProgress(reported);

        await sut.ReclassifyAllAsync(progress, TestContext.Current.CancellationToken);

        reported.ShouldBe([ReclassificationProgressFactory.Create(1, 2), ReclassificationProgressFactory.Create(2, 2)]);
    }

    [Fact]
    public async Task when_run_then_the_work_does_not_happen_on_the_calling_thread()
    {
        await AddFileAsync("/Pictures/one.jpg");
        int callingThreadId = 0;
        List<int> reportingThreadIds = [];
        var progress = new ThreadRecordingProgress(reportingThreadIds);
        var callingThread = new Thread(() =>
        {
            callingThreadId = Environment.CurrentManagedThreadId;
            _ = sut.ReclassifyAllAsync(progress, TestContext.Current.CancellationToken).GetAwaiter().GetResult();
        });

        callingThread.Start();
        callingThread.Join();

        reportingThreadIds.ShouldNotBeEmpty();
        reportingThreadIds.ShouldNotContain(callingThreadId);
    }

    [Fact]
    public async Task when_cancelled_then_operation_cancelled_is_thrown()
    {
        await AddFileAsync("/Pictures/holiday.jpg");
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.ReclassifyAllAsync(new Progress<ReclassificationProgress>(), cancellation.Token));
    }

    private sealed class SynchronousProgress(List<ReclassificationProgress> reported) : IProgress<ReclassificationProgress>
    {
        public void Report(ReclassificationProgress value) => reported.Add(value);
    }

    private sealed class ThreadRecordingProgress(List<int> threadIds) : IProgress<ReclassificationProgress>
    {
        public void Report(ReclassificationProgress value) => threadIds.Add(Environment.CurrentManagedThreadId);
    }
}
