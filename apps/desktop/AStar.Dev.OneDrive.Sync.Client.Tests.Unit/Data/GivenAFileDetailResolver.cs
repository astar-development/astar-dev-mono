using AStar.Dev.Infrastructure.AppDb;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Data;

public sealed class GivenAFileDetailResolver : IAsyncLifetime
{
    private SqliteConnection connection = null!;
    private DbContextOptions<AppDbContext> options = null!;
    private FileDetailResolver sut = null!;

    public async ValueTask InitializeAsync()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var seedContext = new AppDbContext(options);
        await seedContext.Database.EnsureCreatedAsync();

        var factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(new AppDbContext(options)));

        sut = new FileDetailResolver(factory);
    }

    public async ValueTask DisposeAsync() => await connection.DisposeAsync();

    [Fact]
    public async Task when_no_matching_file_exists_then_a_file_detail_is_created()
    {
        var fileDetail = await sut.FindOrCreateAsync("/sync-root/Photos/beach.jpg", 2048, TestContext.Current.CancellationToken);

        await using var verifyContext = new AppDbContext(options);
        var stored = await verifyContext.Files.SingleAsync(TestContext.Current.CancellationToken);
        stored.Id.ShouldBe(fileDetail.Id);
        stored.FileName.Value.ShouldBe("beach.jpg");
        stored.DirectoryName.Value.ShouldBe("/sync-root/Photos");
        stored.FileSize.ShouldBe(2048);
    }

    [Fact]
    public async Task when_a_matching_file_exists_then_it_is_returned_without_creating_another()
    {
        await using var seedContext = new AppDbContext(options);
        var existing = new FileDetailEntity
        {
            FileName = new FileName("beach.jpg"),
            DirectoryName = new DirectoryName("/sync-root/Photos"),
            FileHandle = new FileHandle("existing-handle")
        };
        seedContext.Files.Add(existing);
        await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var fileDetail = await sut.FindOrCreateAsync("/sync-root/Photos/beach.jpg", null, TestContext.Current.CancellationToken);

        fileDetail.Id.ShouldBe(existing.Id);
        await using var verifyContext = new AppDbContext(options);
        (await verifyContext.Files.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
    }

    [Fact]
    public async Task when_called_twice_with_the_same_path_then_only_one_file_detail_exists()
    {
        var first = await sut.FindOrCreateAsync("/sync-root/Docs/report.pdf", 100, TestContext.Current.CancellationToken);
        var second = await sut.FindOrCreateAsync("/sync-root/Docs/report.pdf", 100, TestContext.Current.CancellationToken);

        second.Id.ShouldBe(first.Id);
        await using var verifyContext = new AppDbContext(options);
        (await verifyContext.Files.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
    }

    [Fact]
    public async Task when_the_path_uses_windows_separators_then_file_name_and_directory_are_split_correctly()
    {
        var fileDetail = await sut.FindOrCreateAsync(@"C:\Users\test\Pictures\sunset.png", null, TestContext.Current.CancellationToken);

        await using var verifyContext = new AppDbContext(options);
        var stored = await verifyContext.Files.SingleAsync(TestContext.Current.CancellationToken);
        stored.Id.ShouldBe(fileDetail.Id);
        stored.FileName.Value.ShouldBe("sunset.png");
        stored.DirectoryName.Value.ShouldBe(@"C:\Users\test\Pictures");
    }

    [Fact]
    public async Task when_no_file_size_is_supplied_then_the_created_file_detail_has_zero_size()
    {
        await sut.FindOrCreateAsync("/sync-root/Docs/empty.txt", null, TestContext.Current.CancellationToken);

        await using var verifyContext = new AppDbContext(options);
        var stored = await verifyContext.Files.SingleAsync(TestContext.Current.CancellationToken);
        stored.FileSize.ShouldBe(0);
    }
}
