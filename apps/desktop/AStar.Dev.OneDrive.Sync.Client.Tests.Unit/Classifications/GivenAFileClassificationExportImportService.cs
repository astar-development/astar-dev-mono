using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Classifications;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using System.Text.Json;
using Testably.Abstractions.Testing;

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
        repository.GetKeywordsForCategoryAsync(Arg.Any<FileClassificationCategoryId>(), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<IReadOnlyList<FileClassificationKeywordEntry>>([]));
        repository.AddCategoryAsync(Arg.Any<FileClassificationCategory>(), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<Result<FileClassificationCategoryId, string>>(new Result<FileClassificationCategoryId, string>.Ok(new FileClassificationCategoryId(1))));
        repository.DeleteAllAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.CompletedTask);

        sut = new FileClassificationExportImportService(repository, fileSystem);
    }

    [Fact]
    public async Task when_exporting_empty_taxonomy_then_json_has_empty_categories_array()
    {
        await sut.ExportAsync(ExportFilePath, CancellationToken.None);

        var written = fileSystem.File.ReadAllText(ExportFilePath);
        using var doc = JsonDocument.Parse(written);
        doc.RootElement.GetProperty("categories").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task when_exporting_with_root_categories_then_json_contains_category_names()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Photos", 1, Option.None<FileClassificationCategoryId>()),
            new(new FileClassificationCategoryId(2), "Documents", 1, Option.None<FileClassificationCategoryId>())
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));

        await sut.ExportAsync(ExportFilePath, CancellationToken.None);

        var written = fileSystem.File.ReadAllText(ExportFilePath);
        using var doc = JsonDocument.Parse(written);
        var categoryNames = doc.RootElement.GetProperty("categories")
                               .EnumerateArray()
                               .Select(e => e.GetProperty("name").GetString())
                               .ToList();
        categoryNames.ShouldContain("Photos");
        categoryNames.ShouldContain("Documents");
    }

    [Fact]
    public async Task when_exporting_with_nested_categories_then_json_reflects_hierarchy()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Photos", 1, Option.None<FileClassificationCategoryId>()),
            new(new FileClassificationCategoryId(2), "Holidays", 2, Option.Some(new FileClassificationCategoryId(1)))
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));

        await sut.ExportAsync(ExportFilePath, CancellationToken.None);

        var written = fileSystem.File.ReadAllText(ExportFilePath);
        using var doc = JsonDocument.Parse(written);
        var rootCategories = doc.RootElement.GetProperty("categories").EnumerateArray().ToList();
        rootCategories.Count.ShouldBe(1);
        var photosChildren = rootCategories[0].GetProperty("children").EnumerateArray().ToList();
        photosChildren.Count.ShouldBe(1);
        photosChildren[0].GetProperty("name").GetString().ShouldBe("Holidays");
    }

    [Fact]
    public async Task when_exporting_with_keywords_then_json_contains_keyword_values()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Photos", 1, Option.None<FileClassificationCategoryId>())
        ];
        IReadOnlyList<FileClassificationKeywordEntry> keywords =
        [
            new FileClassificationKeywordEntry(1, new FileClassificationKeyword("holiday", Option.None<bool>())),
            new FileClassificationKeywordEntry(2, new FileClassificationKeyword("vacation", Option.None<bool>()))
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        repository.GetKeywordsForCategoryAsync(new FileClassificationCategoryId(1), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(keywords));

        await sut.ExportAsync(ExportFilePath, CancellationToken.None);

        var written = fileSystem.File.ReadAllText(ExportFilePath);
        using var doc = JsonDocument.Parse(written);
        var keywordElements = doc.RootElement.GetProperty("categories")
                                 .EnumerateArray()
                                 .First()
                                 .GetProperty("keywords")
                                 .EnumerateArray()
                                 .ToList();
        keywordElements.Select(e => e.GetProperty("value").GetString()).ShouldContain("holiday");
        keywordElements.Select(e => e.GetProperty("value").GetString()).ShouldContain("vacation");
    }

    [Fact]
    public async Task when_exporting_keyword_with_is_special_override_true_then_json_preserves_flag()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Photos", 1, Option.None<FileClassificationCategoryId>())
        ];
        IReadOnlyList<FileClassificationKeywordEntry> keywords =
        [
            new FileClassificationKeywordEntry(1, new FileClassificationKeyword("holiday", Option.Some(true)))
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        repository.GetKeywordsForCategoryAsync(new FileClassificationCategoryId(1), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(keywords));

        await sut.ExportAsync(ExportFilePath, CancellationToken.None);

        var written = fileSystem.File.ReadAllText(ExportFilePath);
        using var doc = JsonDocument.Parse(written);
        var keywordEl = doc.RootElement.GetProperty("categories")
                           .EnumerateArray()
                           .First()
                           .GetProperty("keywords")
                           .EnumerateArray()
                           .Single();
        keywordEl.GetProperty("value").GetString().ShouldBe("holiday");
        keywordEl.GetProperty("isSpecialOverride").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task when_exporting_keyword_with_no_is_special_override_then_json_has_null_flag()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Photos", 1, Option.None<FileClassificationCategoryId>())
        ];
        IReadOnlyList<FileClassificationKeywordEntry> keywords =
        [
            new FileClassificationKeywordEntry(1, new FileClassificationKeyword("holiday", Option.None<bool>()))
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        repository.GetKeywordsForCategoryAsync(new FileClassificationCategoryId(1), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(keywords));

        await sut.ExportAsync(ExportFilePath, CancellationToken.None);

        var written = fileSystem.File.ReadAllText(ExportFilePath);
        using var doc = JsonDocument.Parse(written);
        var keywordEl = doc.RootElement.GetProperty("categories")
                           .EnumerateArray()
                           .First()
                           .GetProperty("keywords")
                           .EnumerateArray()
                           .Single();
        keywordEl.GetProperty("isSpecialOverride").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task when_importing_then_delete_all_called_before_any_add()
    {
        var callOrder = new List<string>();
        repository.DeleteAllAsync(Arg.Any<CancellationToken>())
                  .Returns(_ => { callOrder.Add("delete"); return Task.CompletedTask; });
        repository.AddCategoryAsync(Arg.Any<FileClassificationCategory>(), Arg.Any<CancellationToken>())
                  .Returns(_ => { callOrder.Add("add"); return Task.FromResult<Result<FileClassificationCategoryId, string>>(new Result<FileClassificationCategoryId, string>.Ok(new FileClassificationCategoryId(1))); });

        var importJson = """{"version":1,"categories":[{"name":"Photos","children":[],"keywords":[]}]}""";
        fileSystem.File.WriteAllText(ExportFilePath, importJson);

        await sut.ImportAsync(ExportFilePath, CancellationToken.None);

        callOrder[0].ShouldBe("delete");
    }

    [Fact]
    public async Task when_importing_root_category_then_add_category_called()
    {
        var importJson = """{"version":1,"categories":[{"name":"Photos","children":[],"keywords":[]}]}""";
        fileSystem.File.WriteAllText(ExportFilePath, importJson);

        await sut.ImportAsync(ExportFilePath, CancellationToken.None);

        await repository.Received(1).AddCategoryAsync(Arg.Is<FileClassificationCategory>(c => c.Name == "Photos"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_importing_nested_category_then_child_added_with_parent_id()
    {
        var parentId = new FileClassificationCategoryId(42);
        repository.AddCategoryAsync(Arg.Is<FileClassificationCategory>(c => c.Name == "Photos" && c.Level == 1), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<Result<FileClassificationCategoryId, string>>(new Result<FileClassificationCategoryId, string>.Ok(parentId)));

        var importJson = """{"version":1,"categories":[{"name":"Photos","children":[{"name":"Holidays","children":[],"keywords":[]}],"keywords":[]}]}""";
        fileSystem.File.WriteAllText(ExportFilePath, importJson);

        await sut.ImportAsync(ExportFilePath, CancellationToken.None);

        await repository.Received(1).AddCategoryAsync(
            Arg.Is<FileClassificationCategory>(c => c.Name == "Holidays" && c.ParentId == Option.Some(parentId)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_importing_keyword_then_add_keyword_called_for_leaf_category()
    {
        var leafCategoryId = new FileClassificationCategoryId(7);
        repository.AddCategoryAsync(Arg.Is<FileClassificationCategory>(c => c.Name == "Photos"), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<Result<FileClassificationCategoryId, string>>(new Result<FileClassificationCategoryId, string>.Ok(leafCategoryId)));
        repository.AddKeywordAsync(Arg.Any<FileClassificationCategoryId>(), Arg.Any<FileClassificationKeyword>(), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(1)));

        var importJson = """{"version":1,"categories":[{"name":"Photos","children":[],"keywords":[{"value":"holiday","isSpecialOverride":null},{"value":"vacation","isSpecialOverride":null}]}]}""";
        fileSystem.File.WriteAllText(ExportFilePath, importJson);

        await sut.ImportAsync(ExportFilePath, CancellationToken.None);

        await repository.Received(1).AddKeywordAsync(leafCategoryId, Arg.Is<FileClassificationKeyword>(k => k.Value == "holiday"), Arg.Any<CancellationToken>());
        await repository.Received(1).AddKeywordAsync(leafCategoryId, Arg.Is<FileClassificationKeyword>(k => k.Value == "vacation"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_importing_keyword_with_is_special_override_true_then_flag_restored()
    {
        var leafCategoryId = new FileClassificationCategoryId(7);
        repository.AddCategoryAsync(Arg.Is<FileClassificationCategory>(c => c.Name == "Photos"), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<Result<FileClassificationCategoryId, string>>(new Result<FileClassificationCategoryId, string>.Ok(leafCategoryId)));
        repository.AddKeywordAsync(Arg.Any<FileClassificationCategoryId>(), Arg.Any<FileClassificationKeyword>(), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(1)));

        var importJson = """{"version":1,"categories":[{"name":"Photos","children":[],"keywords":[{"value":"holiday","isSpecialOverride":true}]}]}""";
        fileSystem.File.WriteAllText(ExportFilePath, importJson);

        await sut.ImportAsync(ExportFilePath, CancellationToken.None);

        await repository.Received(1).AddKeywordAsync(leafCategoryId, Arg.Is<FileClassificationKeyword>(k => k.Value == "holiday" && k.IsSpecialOverride == Option.Some(true)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_importing_keyword_with_null_is_special_override_then_flag_is_none()
    {
        var leafCategoryId = new FileClassificationCategoryId(7);
        repository.AddCategoryAsync(Arg.Is<FileClassificationCategory>(c => c.Name == "Photos"), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<Result<FileClassificationCategoryId, string>>(new Result<FileClassificationCategoryId, string>.Ok(leafCategoryId)));
        repository.AddKeywordAsync(Arg.Any<FileClassificationCategoryId>(), Arg.Any<FileClassificationKeyword>(), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(1)));

        var importJson = """{"version":1,"categories":[{"name":"Photos","children":[],"keywords":[{"value":"holiday","isSpecialOverride":null}]}]}""";
        fileSystem.File.WriteAllText(ExportFilePath, importJson);

        await sut.ImportAsync(ExportFilePath, CancellationToken.None);

        await repository.Received(1).AddKeywordAsync(leafCategoryId, Arg.Is<FileClassificationKeyword>(k => k.Value == "holiday" && k.IsSpecialOverride == Option.None<bool>()), Arg.Any<CancellationToken>());
    }
}
