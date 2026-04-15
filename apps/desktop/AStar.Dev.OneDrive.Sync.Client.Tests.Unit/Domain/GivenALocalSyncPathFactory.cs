using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

public sealed class GivenALocalSyncPathFactory
{
    [Fact]
    public void when_created_with_valid_path_then_result_is_success()
    {
        var result = LocalSyncPathFactory.Create("/home/user/OneDrive");

        _ = result.ShouldBeOfType<Result<LocalSyncPath, ErrorResponse>.Ok>();
    }

    [Fact]
    public void when_created_with_valid_path_then_value_matches_raw_path()
    {
        const string rawPath = "/home/user/OneDrive";

        var result = LocalSyncPathFactory.Create(rawPath);

        result.Match(p => p.Value, _ => string.Empty).ShouldBe(rawPath);
    }

    [Fact]
    public void when_created_with_null_then_result_is_failure()
    {
        var result = LocalSyncPathFactory.Create(null);

        _ = result.ShouldBeOfType<Result<LocalSyncPath, ErrorResponse>.Error>();
    }

    [Fact]
    public void when_created_with_empty_string_then_result_is_failure()
    {
        var result = LocalSyncPathFactory.Create(string.Empty);

        _ = result.ShouldBeOfType<Result<LocalSyncPath, ErrorResponse>.Error>();
    }

    [Fact]
    public void when_created_with_whitespace_only_then_result_is_failure()
    {
        var result = LocalSyncPathFactory.Create("   ");

        _ = result.ShouldBeOfType<Result<LocalSyncPath, ErrorResponse>.Error>();
    }

    [Fact]
    public void when_created_with_null_then_error_message_describes_constraint()
    {
        var result = LocalSyncPathFactory.Create(null);

        result.Match(
            _ => string.Empty,
            err => err.Message)
            .ShouldNotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("C:\\Users\\user\\OneDrive")]
    [InlineData("/home/user/OneDrive")]
    [InlineData("D:\\OneDrive Backup")]
    [InlineData("/var/sync")]
    public void when_created_with_various_valid_paths_then_result_is_success(string path)
    {
        var result = LocalSyncPathFactory.Create(path);

        _ = result.ShouldBeOfType<Result<LocalSyncPath, ErrorResponse>.Ok>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void when_created_with_invalid_path_then_result_is_failure(string? path)
    {
        var result = LocalSyncPathFactory.Create(path);

        _ = result.ShouldBeOfType<Result<LocalSyncPath, ErrorResponse>.Error>();
    }
}
