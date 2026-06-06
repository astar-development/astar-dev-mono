using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Classifications;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Classifications;

public sealed class GivenAFileClassificationRulesViewModel
{
    private readonly IFileClassificationRepository repository;

    public GivenAFileClassificationRulesViewModel()
    {
        repository = Substitute.For<IFileClassificationRepository>();
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<IReadOnlyList<FileClassificationCategory>>([]));
        repository.GetKeywordsForCategoryAsync(Arg.Any<FileClassificationCategoryId>(), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<IReadOnlyList<FileClassificationKeywordEntry>>([]));
        repository.AddCategoryAsync(Arg.Any<FileClassificationCategory>(), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<Result<FileClassificationCategoryId, string>>(new Result<FileClassificationCategoryId, string>.Ok(new FileClassificationCategoryId(1))));
    }

    [Fact]
    public async Task when_load_async_called_then_level1_categories_populated()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Media", 1, Option.None<FileClassificationCategoryId>()),
            new(new FileClassificationCategoryId(2), "Documents", 1, Option.None<FileClassificationCategoryId>())
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        FileClassificationRulesViewModel sut = new(repository);

        await sut.LoadAsync(CancellationToken.None);

        sut.Categories.Count.ShouldBe(2);
    }

    [Fact]
    public async Task when_load_async_called_then_child_categories_nested_under_parent()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Media", 1, Option.None<FileClassificationCategoryId>()),
            new(new FileClassificationCategoryId(2), "Photos", 2, Option.Some(new FileClassificationCategoryId(1)))
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        FileClassificationRulesViewModel sut = new(repository);

        await sut.LoadAsync(CancellationToken.None);

        sut.Categories.Count.ShouldBe(1);
        sut.Categories[0].Children.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_load_async_called_then_keywords_loaded_for_leaf_categories()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Media", 1, Option.None<FileClassificationCategoryId>())
        ];
        IReadOnlyList<FileClassificationKeywordEntry> keywords =
        [
            new FileClassificationKeywordEntry(1, new FileClassificationKeyword("cats", Option.None<bool>()))
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        repository.GetKeywordsForCategoryAsync(Arg.Any<FileClassificationCategoryId>(), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(keywords));
        FileClassificationRulesViewModel sut = new(repository);

        await sut.LoadAsync(CancellationToken.None);

        sut.Categories[0].Keywords.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_add_category_command_executed_then_category_persisted_and_added()
    {
        FileClassificationRulesViewModel sut = new(repository)
        {
            NewCategoryName = "Media"
        };

        await sut.AddCategoryCommand.ExecuteAsync(null);

        await repository.Received(1).AddCategoryAsync(Arg.Any<FileClassificationCategory>(), Arg.Any<CancellationToken>());
        sut.Categories.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_add_category_command_executed_then_new_category_name_cleared()
    {
        FileClassificationRulesViewModel sut = new(repository)
        {
            NewCategoryName = "Media"
        };

        await sut.AddCategoryCommand.ExecuteAsync(null);

        sut.NewCategoryName.ShouldBeEmpty();
    }

    [Fact]
    public void when_new_category_name_empty_then_add_category_command_disabled()
    {
        FileClassificationRulesViewModel sut = new(repository)
        {
            NewCategoryName = string.Empty
        };

        sut.AddCategoryCommand.CanExecute(null).ShouldBeFalse();
    }

    [Fact]
    public void when_no_categories_then_has_no_categories_is_true()
    {
        FileClassificationRulesViewModel sut = new(repository);

        sut.HasNoCategories.ShouldBeTrue();
    }

    [Fact]
    public async Task when_categories_present_then_has_no_categories_is_false()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Media", 1, Option.None<FileClassificationCategoryId>())
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        FileClassificationRulesViewModel sut = new(repository);

        await sut.LoadAsync(CancellationToken.None);

        sut.HasNoCategories.ShouldBeFalse();
    }
}
