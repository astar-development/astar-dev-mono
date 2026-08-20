using AStar.Dev.File.App.Data;
using AStar.Dev.File.App.Models;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.File.App.TestsUnit.Data;

public class GivenAFileAppDbContext
{
    [Fact]
    public async Task when_saving_a_scanned_file_then_it_can_be_read_back()
    {
        await using var sut = CreateSut();

        sut.ScannedFiles.Add(MakeScannedFile("/data/docs/report.pdf"));
        await sut.SaveChangesAsync(TestContext.Current.CancellationToken);

        var saved = await sut.ScannedFiles.SingleAsync(TestContext.Current.CancellationToken);
        saved.FullPath.ShouldBe("/data/docs/report.pdf");
        saved.FileType.ShouldBe(FileType.Document);
    }

    [Fact]
    public void when_the_model_is_built_then_the_scanned_file_full_path_index_is_unique()
    {
        using var sut = CreateSut();

        var index = sut.Model.FindEntityType(typeof(ScannedFile))!.FindIndex(sut.Model.FindEntityType(typeof(ScannedFile))!.FindProperty(nameof(ScannedFile.FullPath))!);

        index!.IsUnique.ShouldBeTrue();
    }

    [Fact]
    public void when_the_model_is_built_then_the_app_setting_key_index_is_unique()
    {
        using var sut = CreateSut();

        var index = sut.Model.FindEntityType(typeof(AppSetting))!.FindIndex(sut.Model.FindEntityType(typeof(AppSetting))!.FindProperty(nameof(AppSetting.Key))!);

        index!.IsUnique.ShouldBeTrue();
    }

    [Fact]
    public void when_the_model_is_built_then_the_scanned_file_size_in_bytes_index_exists()
    {
        using var sut = CreateSut();

        var index = sut.Model.FindEntityType(typeof(ScannedFile))!.FindIndex(sut.Model.FindEntityType(typeof(ScannedFile))!.FindProperty(nameof(ScannedFile.SizeInBytes))!);

        index.ShouldNotBeNull();
    }

    [Fact]
    public async Task when_saving_an_app_setting_then_it_can_be_read_back()
    {
        await using var sut = CreateSut();

        sut.AppSettings.Add(new AppSetting { Key = "SelectedFolderPath", Value = "/data/photos" });
        await sut.SaveChangesAsync(TestContext.Current.CancellationToken);

        var saved = await sut.AppSettings.SingleAsync(TestContext.Current.CancellationToken);
        saved.Key.ShouldBe("SelectedFolderPath");
        saved.Value.ShouldBe("/data/photos");
    }

    private static FileAppDbContext CreateSut()
    {
        var options = new DbContextOptionsBuilder<FileAppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new FileAppDbContext(options);
    }

    private static ScannedFile MakeScannedFile(string fullPath) => new()
    {
        RootPath = "/data",
        FolderPath = "/data/docs",
        FileName = Path.GetFileName(fullPath),
        FullPath = fullPath,
        FileType = FileType.Document,
        LastModified = DateTime.UtcNow
    };
}
