using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Classifications;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Classifications;

public sealed class GivenACategoryNodeViewModel
{
    private readonly IFileClassificationRepository repository;

    public GivenACategoryNodeViewModel()
    {
        repository = Substitute.For<IFileClassificationRepository>();
        repository.AddCategoryAsync(Arg.Any<FileClassificationCategory>(), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<Result<FileClassificationCategoryId, string>>(new Result<FileClassificationCategoryId, string>.Ok(new FileClassificationCategoryId(42))));
    }

    private CategoryNodeViewModel CreateSut(int level = 1) =>
        new(new FileClassificationCategoryId(1), "Media", level, false, false, Option.None<FileClassificationCategoryId>(), repository, _ => { });

    [Fact]
    public async Task when_add_child_category_command_executed_then_category_persisted_and_child_added()
    {
        var sut = CreateSut();
        sut.NewChildCategoryName = "Photos";

        await sut.AddChildCategoryCommand.ExecuteAsync(null);

        await repository.Received(1).AddCategoryAsync(Arg.Any<FileClassificationCategory>(), Arg.Any<CancellationToken>());
        sut.Children.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_add_child_category_command_executed_then_new_child_name_cleared()
    {
        var sut = CreateSut();
        sut.NewChildCategoryName = "Photos";

        await sut.AddChildCategoryCommand.ExecuteAsync(null);

        sut.NewChildCategoryName.ShouldBeEmpty();
    }

    [Fact]
    public void when_level_is_3_then_add_child_category_command_disabled()
    {
        var sut = CreateSut(level: 3);
        sut.NewChildCategoryName = "Deep";

        sut.AddChildCategoryCommand.CanExecute(null).ShouldBeFalse();
    }

    [Fact]
    public async Task when_delete_self_command_executed_then_on_delete_self_callback_invoked()
    {
        bool callbackInvoked = false;
        CategoryNodeViewModel sut = new(new FileClassificationCategoryId(1), "Media", 1, false, false, Option.None<FileClassificationCategoryId>(), repository, _ => callbackInvoked = true);

        await sut.DeleteSelfCommand.ExecuteAsync(null);

        callbackInvoked.ShouldBeTrue();
    }
}
