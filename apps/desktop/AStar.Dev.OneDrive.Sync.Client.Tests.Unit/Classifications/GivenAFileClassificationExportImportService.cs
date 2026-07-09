using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Classifications;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using System.Text.Json;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Classifications;

public sealed class GivenAFileClassificationExportImportService
{
    private const string ExportFilePath = "/export/classifications.json";

    private readonly IFileClassificationRepository repository;
    private readonly MockFileSystem fileSystem;
    private readonly FileClassificationExportImportService sut;

    public GivenAFileClassificationExportImportService()
    {
        repository = Substitute.For<IFileClassificationRepository>();
        fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory("/export");

        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<IReadOnlyList<FileClassificationCategory>>([]));
        repository.AddCategoryAsync(Arg.Any<FileClassificationCategory>(), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<Result<FileClassificationCategoryId, string>>(new Result<FileClassificationCategoryId, string>.Ok(new FileClassificationCategoryId(1))));
        sut = new FileClassificationExportImportService(repository, fileSystem);
    }

    [Fact]
    public async Task when_exporting_empty_taxonomy_then_json_has_empty_categories_array()
    {
        await sut.ExportAsync(fileSystem.FileInfo.New(ExportFilePath), CancellationToken.None);

        string written = fileSystem.File.ReadAllText(ExportFilePath);
        using var doc = JsonDocument.Parse(written);

        doc.RootElement.GetProperty("categories").EnumerateArray().Count().ShouldBe(0);
    }

    [Fact]
    public async Task when_exporting_with_root_categories_then_json_contains_category_names()
    {
        IReadOnlyList<FileClassificationCategoryEntity> categories =
        [
            new(){Id = 1, Name = "Photos", Level = 1, IsFamous = false, IsInternet = false, ParentId = null, IncludeInSearch = false},
            new(){Id = 2, Name = "Documents", Level = 1, IsFamous = false, IsInternet = false, ParentId = null, IncludeInSearch = false}
        ];
        repository.GetAllCategoriesSimpleAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));

        await sut.ExportAsync(fileSystem.FileInfo.New(ExportFilePath), CancellationToken.None);

        string written = fileSystem.File.ReadAllText(ExportFilePath);

        written.ShouldContain("Photos");
        written.ShouldContain("Documents");
    }

    [Fact]
    public async Task when_exporting_with_nested_categories_then_json_reflects_hierarchy()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Photos", 1, false, false, Option.None<FileClassificationCategoryId>(), false),
            new(new FileClassificationCategoryId(2), "Holidays", 2, false, false, Option.Some(new FileClassificationCategoryId(1)), false)
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));

        await sut.ExportAsync(fileSystem.FileInfo.New(ExportFilePath), CancellationToken.None);

        string written = fileSystem.File.ReadAllText(ExportFilePath);
        using var doc = JsonDocument.Parse(written);
        var rootCategories = doc.RootElement.GetProperty("categories").EnumerateArray().ToList();
        rootCategories.Count.ShouldBe(1);
        var photosChildren = rootCategories[0].GetProperty("children").EnumerateArray().ToList();
        photosChildren.Count.ShouldBe(1);
        photosChildren[0].GetProperty("name").GetString().ShouldBe("Holidays");
    }

    [Fact]
    public async Task when_importing_root_category_then_add_category_called()
    {
        string importJson = """{"version":1,"categories":[{"name":"Photos","children":[],"keywords":[]}]}""";
        fileSystem.File.WriteAllText(ExportFilePath, importJson);

        await sut.ImportAsync(fileSystem.FileInfo.New(ExportFilePath), CancellationToken.None);

        await repository.Received(1).AddCategoryAsync(Arg.Is<FileClassificationCategory>(c => c.Name == "Photos"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_importing_nested_category_then_child_added_with_parent_id()
    {
        var parentId = new FileClassificationCategoryId(42);
        repository.AddCategoryAsync(Arg.Is<FileClassificationCategory>(c => c.Name == "Photos" && c.Level == 1), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<Result<FileClassificationCategoryId, string>>(new Result<FileClassificationCategoryId, string>.Ok(parentId)));

        string importJson = """{"version":1,"categories":[{"name":"Photos","id":42,"children":[{"name":"Holidays","parentId":42,"id":12,"children":[],"keywords":[]}],"keywords":[]}]}""";
        fileSystem.File.WriteAllText(ExportFilePath, importJson);

        await sut.ImportAsync(fileSystem.FileInfo.New(ExportFilePath), CancellationToken.None);

        await repository.Received(1).AddCategoryAsync(
            Arg.Is<FileClassificationCategory>(c => c.Name == "Holidays" && c.ParentId == Option.Some(parentId)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_importing_an_existing_category_then_add_category_is_not_called()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Photos", 1, false, false, Option.None<FileClassificationCategoryId>(), false),
            new(new FileClassificationCategoryId(2), "Documents", 1, false, false, Option.None<FileClassificationCategoryId>(), false)
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));

        string importJson = """{"version":1,"categories":[{"name":"Photos","id":1,"children":[],"keywords":[]}]}""";
        fileSystem.File.WriteAllText(ExportFilePath, importJson);

        await sut.ImportAsync(fileSystem.FileInfo.New(ExportFilePath), CancellationToken.None);

        await repository.DidNotReceive().AddCategoryAsync(Arg.Is<FileClassificationCategory>(c => c.Name == "Photos"), Arg.Any<CancellationToken>());
    }
}
