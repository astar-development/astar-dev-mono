using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Client.Features.FolderBrowsing;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.Features.FolderBrowsing;

public sealed class GivenAnOneDriveFolderService
{
    private readonly IOneDriveFolderService _sut = Substitute.For<IOneDriveFolderService>();

    [Fact]
    public async Task when_get_root_folders_is_called_with_valid_token_then_returns_success()
    {
        IReadOnlyList<OneDriveFolder> folders = [OneDriveFolderFactory.Create("id1", "Documents", null, true)];
        _sut.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Result<IReadOnlyList<OneDriveFolder>, string>.Ok(folders));

        var result = await _sut.GetRootFoldersAsync("valid-token", TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Result<IReadOnlyList<OneDriveFolder>, string>.Ok>().Value.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task when_get_root_folders_is_called_with_empty_token_then_returns_failure()
    {
        _sut.GetRootFoldersAsync(string.Empty, Arg.Any<CancellationToken>())
            .Returns(new Result<IReadOnlyList<OneDriveFolder>, string>.Error("Token must not be empty"));

        var result = await _sut.GetRootFoldersAsync(string.Empty, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Result<IReadOnlyList<OneDriveFolder>, string>.Error>().Reason.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task when_get_child_folders_is_called_with_valid_token_and_folder_id_then_returns_success()
    {
        IReadOnlyList<OneDriveFolder> children = [OneDriveFolderFactory.Create("child1", "Sub", "id1", false)];
        _sut.GetChildFoldersAsync("valid-token", "id1", Arg.Any<CancellationToken>())
            .Returns(new Result<IReadOnlyList<OneDriveFolder>, string>.Ok(children));

        var result = await _sut.GetChildFoldersAsync("valid-token", "id1", TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Result<IReadOnlyList<OneDriveFolder>, string>.Ok>().Value.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task when_get_child_folders_returns_empty_then_list_is_empty()
    {
        IReadOnlyList<OneDriveFolder> empty = [];
        _sut.GetChildFoldersAsync("valid-token", "leaf-id", Arg.Any<CancellationToken>())
            .Returns(new Result<IReadOnlyList<OneDriveFolder>, string>.Ok(empty));

        var result = await _sut.GetChildFoldersAsync("valid-token", "leaf-id", TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Result<IReadOnlyList<OneDriveFolder>, string>.Ok>().Value.ShouldBeEmpty();
    }
}
