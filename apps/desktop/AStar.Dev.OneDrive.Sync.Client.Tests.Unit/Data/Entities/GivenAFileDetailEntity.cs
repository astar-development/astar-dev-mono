using AStar.Dev.Infrastructure.AppDb;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Data.Entities;

public sealed class GivenAFileDetailEntity : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly AppDbContext context;

    public GivenAFileDetailEntity()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        context = new AppDbContext(options);
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        context.Dispose();
        connection.Dispose();
    }

    private static FileDetailEntity CreateFileDetail(string handle = "handle-1") =>
        new()
        {
            FileName = new FileName("wallpaper.jpg"),
            DirectoryName = new DirectoryName("/pictures"),
            FileHandle = new FileHandle(handle),
            FileSize = 1024,
            IsImage = true,
            Width = 1920,
            Height = 1080,
            FileAccessDetail = new FileAccessDetailEntity { MoveRequired = true },
            ImageDetail = new ImageDetailEntity { Width = 1920, Height = 1080 },
            DeletionStatus = new DeletionStatusEntity()
        };

    [Fact]
    public async Task when_a_file_detail_is_added_then_it_can_be_retrieved()
    {
        context.Files.Add(CreateFileDetail());
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var retrieved = context.Files.First();

        retrieved.FileName.Value.ShouldBe("wallpaper.jpg");
        retrieved.DirectoryName.Value.ShouldBe("/pictures");
        retrieved.FileHandle.Value.ShouldBe("handle-1");
        retrieved.FileSize.ShouldBe(1024);
    }

    [Fact]
    public async Task when_a_file_detail_is_added_then_its_owned_file_access_detail_is_persisted()
    {
        context.Files.Add(CreateFileDetail());
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var retrieved = context.Files.Include(file => file.FileAccessDetail).First();

        retrieved.FileAccessDetail.MoveRequired.ShouldBeTrue();
    }

    [Fact]
    public async Task when_a_file_detail_is_added_then_its_owned_image_detail_is_persisted()
    {
        context.Files.Add(CreateFileDetail());
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var retrieved = context.Files.Include(file => file.ImageDetail).First();

        retrieved.ImageDetail.Width.ShouldBe(1920);
        retrieved.ImageDetail.Height.ShouldBe(1080);
    }

    [Fact]
    public async Task when_two_file_details_share_a_file_handle_then_save_fails()
    {
        context.Files.Add(CreateFileDetail("duplicate-handle"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.Files.Add(CreateFileDetail("duplicate-handle"));

        await Should.ThrowAsync<DbUpdateException>(async () => await context.SaveChangesAsync(TestContext.Current.CancellationToken));
    }
}
