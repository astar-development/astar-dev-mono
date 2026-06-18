using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Classifications;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Classifications;

public sealed class GivenAKeywordRowViewModel
{
    private readonly IFileClassificationRepository repository;

    public GivenAKeywordRowViewModel()
    {
        repository = Substitute.For<IFileClassificationRepository>();
        repository.UpdateKeywordAsync(Arg.Any<int>(), Arg.Any<FileClassificationKeyword>(), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(1)));
        repository.DeleteKeywordAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                  .Returns(Task.CompletedTask);
    }

    private static KeywordRowViewModel CreateSut(IFileClassificationRepository repo, string value = "cats", bool isFamous = false, bool isInternet = false) =>
        new(keywordId: 1, keyword: new FileClassificationKeyword(value, isFamous ? Option.Some(true) : Option.None<bool>(), isInternet ? Option.Some(true) : Option.None<bool>()), repository: repo, onDeleteSelf: _ => { });

    [Fact]
    public async Task when_save_command_executed_with_valid_value_then_repository_update_called()
    {
        var sut = CreateSut(repository);
        sut.Value = "dogs";
        sut.IsEditing = true;

        await sut.SaveCommand.ExecuteAsync(null);

        await repository.Received(1).UpdateKeywordAsync(1, Arg.Is<FileClassificationKeyword>(k => k.Value == "Dogs"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_save_command_executed_with_valid_value_then_is_editing_set_to_false()
    {
        var sut = CreateSut(repository);
        sut.Value = "dogs";
        sut.IsEditing = true;

        await sut.SaveCommand.ExecuteAsync(null);

        sut.IsEditing.ShouldBeFalse();
    }

    [Fact]
    public async Task when_save_command_executed_with_empty_value_then_repository_not_called()
    {
        var sut = CreateSut(repository);
        sut.Value = string.Empty;

        await sut.SaveCommand.ExecuteAsync(null);

        await repository.DidNotReceive().UpdateKeywordAsync(Arg.Any<int>(), Arg.Any<FileClassificationKeyword>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_save_command_executed_with_empty_value_then_is_editing_unchanged()
    {
        var sut = CreateSut(repository);
        sut.IsEditing = true;
        sut.Value = string.Empty;

        await sut.SaveCommand.ExecuteAsync(null);

        sut.IsEditing.ShouldBeTrue();
    }

    [Fact]
    public void when_cancel_command_executed_then_value_restored_to_original()
    {
        var sut = CreateSut(repository, value: "cats");
        sut.Value = "changed";

        sut.CancelCommand.Execute(null);

        sut.Value.ShouldBe("cats");
    }

    [Fact]
    public void when_cancel_command_executed_then_is_editing_set_to_false()
    {
        var sut = CreateSut(repository);
        sut.IsEditing = true;

        sut.CancelCommand.Execute(null);

        sut.IsEditing.ShouldBeFalse();
    }

    [Fact]
    public async Task when_delete_command_executed_then_repository_delete_called()
    {
        var sut = CreateSut(repository);

        await sut.DeleteCommand.ExecuteAsync(null);

        await repository.Received(1).DeleteKeywordAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_delete_command_executed_then_on_delete_self_callback_invoked()
    {
        bool callbackInvoked = false;
        KeywordRowViewModel sut = new(keywordId: 1, keyword: new FileClassificationKeyword("cats", Option.None<bool>(), Option.None<bool>()), repository: repository, onDeleteSelf: _ => callbackInvoked = true);

        await sut.DeleteCommand.ExecuteAsync(null);

        callbackInvoked.ShouldBeTrue();
    }
}
