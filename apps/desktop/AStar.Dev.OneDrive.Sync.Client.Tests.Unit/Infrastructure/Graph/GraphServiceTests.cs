using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Graph;

public sealed class GivenAGraphService
{
    private const string AnyAccessToken = "any-access-token";
    private const string AnyDriveId = "drive-001";
    private const string AnyFolderId = "folder-001";
    private const string AnyItemId = "item-001";
    private const string AnyRemotePath = "/Documents";
    private const string AnyLocalPath = "/home/user/file.txt";

    [Fact]
    public void when_constructed_then_instance_is_not_null()
    {
        var service = new GraphService(Substitute.For<IUploadService>());

        _ = service.ShouldNotBeNull();
    }

    [Fact]
    public void when_constructed_then_it_implements_IGraphService()
    {
        var service = new GraphService(Substitute.For<IUploadService>());

        _ = service.ShouldBeAssignableTo<IGraphService>();
    }

    [Fact]
    public async Task when_get_drive_id_is_called_with_a_pre_cancelled_token_then_operation_is_cancelled()
    {
        var sut = new GraphService(Substitute.For<IUploadService>());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.GetDriveIdAsync(AnyAccessToken, cts.Token));
    }

    [Fact]
    public async Task when_get_root_folders_is_called_with_a_pre_cancelled_token_then_operation_is_cancelled()
    {
        var sut = new GraphService(Substitute.For<IUploadService>());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.GetRootFoldersAsync(AnyAccessToken, cts.Token));
    }

    [Fact]
    public async Task when_get_child_folders_is_called_with_a_pre_cancelled_token_then_operation_is_cancelled()
    {
        var sut = new GraphService(Substitute.For<IUploadService>());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.GetChildFoldersAsync(AnyAccessToken, AnyDriveId, AnyFolderId, cts.Token));
    }

    [Fact]
    public async Task when_get_quota_is_called_with_a_pre_cancelled_token_then_operation_is_cancelled()
    {
        var sut = new GraphService(Substitute.For<IUploadService>());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.GetQuotaAsync(AnyAccessToken, cts.Token));
    }

    [Fact]
    public async Task when_enumerate_folder_is_called_with_a_pre_cancelled_token_then_operation_is_cancelled()
    {
        var sut = new GraphService(Substitute.For<IUploadService>());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.EnumerateFolderAsync(AnyAccessToken, AnyDriveId, AnyFolderId, AnyRemotePath, cts.Token));
    }

    [Fact]
    public async Task when_get_folder_id_by_path_is_called_with_a_pre_cancelled_token_then_operation_is_cancelled()
    {
        var sut = new GraphService(Substitute.For<IUploadService>());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.GetFolderIdByPathAsync(AnyAccessToken, AnyDriveId, AnyRemotePath, cts.Token));
    }

    [Fact]
    public async Task when_get_download_url_is_called_with_a_pre_cancelled_token_then_operation_is_cancelled()
    {
        var sut = new GraphService(Substitute.For<IUploadService>());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.GetDownloadUrlAsync(AnyAccessToken, AnyItemId, cts.Token));
    }

    [Fact]
    public async Task when_upload_file_is_called_with_a_pre_cancelled_token_then_operation_is_cancelled()
    {
        var sut = new GraphService(Substitute.For<IUploadService>());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.UploadFileAsync(AnyAccessToken, AnyLocalPath, AnyRemotePath, AnyFolderId, cts.Token));
    }

    [Fact]
    public async Task when_delete_item_is_called_with_a_pre_cancelled_token_then_operation_is_cancelled()
    {
        var sut = new GraphService(Substitute.For<IUploadService>());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.DeleteItemAsync(AnyAccessToken, AnyItemId, cts.Token));
    }

    [Fact(Skip = "GraphService.BuildClient is private static and hardcodes graph.microsoft.com — no HTTP seam exists to point the SDK at WireMock without refactoring. See: https://github.com/astar-dev/astar-dev-mono/issues/TODO")]
    public async Task when_get_root_folders_returns_mixed_items_then_only_folders_are_returned_ordered_by_name()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "GraphService.BuildClient is private static and hardcodes graph.microsoft.com — no HTTP seam exists to point the SDK at WireMock without refactoring. See: https://github.com/astar-dev/astar-dev-mono/issues/TODO")]
    public async Task when_get_folder_id_by_path_receives_a_404_then_null_is_returned()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "GraphService.BuildClient is private static and hardcodes graph.microsoft.com — no HTTP seam exists to point the SDK at WireMock without refactoring. See: https://github.com/astar-dev/astar-dev-mono/issues/TODO")]
    public async Task when_get_quota_response_has_null_total_then_zero_is_returned_for_both_values()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "GraphService.BuildClient is private static and hardcodes graph.microsoft.com — no HTTP seam exists to point the SDK at WireMock without refactoring. See: https://github.com/astar-dev/astar-dev-mono/issues/TODO")]
    public async Task when_get_download_url_item_has_no_additional_data_then_null_is_returned()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "GraphService.BuildClient is private static and hardcodes graph.microsoft.com — no HTTP seam exists to point the SDK at WireMock without refactoring. See: https://github.com/astar-dev/astar-dev-mono/issues/TODO")]
    public async Task when_enumerate_folder_contains_subfolders_then_recursive_enumeration_visits_each_child()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "GraphService.BuildClient is private static and hardcodes graph.microsoft.com — no HTTP seam exists to point the SDK at WireMock without refactoring. See: https://github.com/astar-dev/astar-dev-mono/issues/TODO")]
    public async Task when_enumerate_folder_encounters_a_cycle_via_parentId_then_each_folder_is_visited_exactly_once()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "GraphService.BuildClient is private static and hardcodes graph.microsoft.com — no HTTP seam exists to point the SDK at WireMock without refactoring. See: https://github.com/astar-dev/astar-dev-mono/issues/TODO")]
    public async Task when_drive_context_is_resolved_twice_with_the_same_token_then_the_me_drive_endpoint_is_called_only_once()
    {
        await Task.CompletedTask;
    }
}
