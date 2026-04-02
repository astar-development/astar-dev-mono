using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDriveSync.Features.Accounts;
using System.IO.Abstractions;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Features.Accounts;

public sealed class GivenLocalSyncPathOverlapDetection
{
    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly LocalSyncPathService _sut;

    public GivenLocalSyncPathOverlapDetection()
    {
        _fileSystem.Path.Combine(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(callInfo => System.IO.Path.Combine(callInfo.ArgAt<string>(0), callInfo.ArgAt<string>(1), callInfo.ArgAt<string>(2)));

        _sut = new LocalSyncPathService(_accountRepository, _fileSystem);
    }

    [Fact]
    public async Task when_candidate_path_does_not_overlap_any_existing_path_then_returns_success()
    {
        _accountRepository.GetAllSyncPathsAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<(Guid, string)>)[(Guid.NewGuid(), "/home/user/OneDrive/Alice")]);

        var result = await _sut.ValidateNoOverlapAsync("/home/user/OneDrive/Bob", ct: TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Result<bool, string>.Ok>().Value.ShouldBeTrue();
    }

    [Fact]
    public async Task when_candidate_path_is_prefix_of_existing_path_then_returns_failure()
    {
        _accountRepository.GetAllSyncPathsAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<(Guid, string)>)[(Guid.NewGuid(), "/home/user/OneDrive/Alice/Sub")]);

        var result = await _sut.ValidateNoOverlapAsync("/home/user/OneDrive/Alice", ct: TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Result<bool, string>.Error>().Reason.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task when_candidate_path_is_contained_within_existing_path_then_returns_failure()
    {
        _accountRepository.GetAllSyncPathsAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<(Guid, string)>)[(Guid.NewGuid(), "/home/user/OneDrive")]);

        var result = await _sut.ValidateNoOverlapAsync("/home/user/OneDrive/Alice", ct: TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Result<bool, string>.Error>().Reason.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task when_candidate_path_matches_excluded_account_then_returns_success()
    {
        var accountId = Guid.NewGuid();
        _accountRepository.GetAllSyncPathsAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<(Guid, string)>)[(accountId, "/home/user/OneDrive/Alice")]);

        var result = await _sut.ValidateNoOverlapAsync("/home/user/OneDrive/Alice", excludeAccountId: accountId, ct: TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Result<bool, string>.Ok>().Value.ShouldBeTrue();
    }

    [Fact]
    public async Task when_no_accounts_exist_then_any_path_returns_success()
    {
        _accountRepository.GetAllSyncPathsAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<(Guid, string)>)[]);

        var result = await _sut.ValidateNoOverlapAsync("/home/user/OneDrive/New", ct: TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Result<bool, string>.Ok>().Value.ShouldBeTrue();
    }

    [Fact]
    public void when_folder_is_empty_then_is_non_empty_returns_false()
    {
        _fileSystem.Directory.Exists("/home/user/empty-folder").Returns(true);
        _fileSystem.Directory.EnumerateFileSystemEntries("/home/user/empty-folder").Returns([]);

        _sut.IsNonEmpty("/home/user/empty-folder").ShouldBeFalse();
    }

    [Fact]
    public void when_folder_contains_files_then_is_non_empty_returns_true()
    {
        _fileSystem.Directory.Exists("/home/user/sync").Returns(true);
        _fileSystem.Directory.EnumerateFileSystemEntries("/home/user/sync").Returns(["/home/user/sync/file.txt"]);

        _sut.IsNonEmpty("/home/user/sync").ShouldBeTrue();
    }

    [Fact]
    public void when_folder_does_not_exist_then_is_non_empty_returns_false()
    {
        _fileSystem.Directory.Exists("/home/user/does-not-exist").Returns(false);

        _sut.IsNonEmpty("/home/user/does-not-exist").ShouldBeFalse();
    }

    [Fact]
    public void when_get_default_path_then_includes_display_name_and_onedrive_segment()
    {
        var path = _sut.GetDefaultPath("Alice Smith");

        path.ShouldContain("Alice Smith");
        path.ShouldContain("OneDrive");
    }
}
