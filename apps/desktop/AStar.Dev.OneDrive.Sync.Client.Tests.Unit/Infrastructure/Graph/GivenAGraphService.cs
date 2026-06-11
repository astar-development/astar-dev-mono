using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Graph;

public sealed class GivenAGraphService : IDisposable
{
    private const string AnyAccountId = "account-001";
    private const string AnyAccessToken = "any-access-token";
    private const string AnyDriveId = "drive-001";
    private const string AnyFolderId = "folder-001";
    private const string AnyItemId = "item-001";
    private const string AnyRemotePath = "/Documents";
    private const string AnyLocalPath = "/home/user/file.txt";

    private readonly WireMockServer _server = WireMockServer.Start();

    public void Dispose() => _server.Stop();

    private GraphService CreateSut() =>
        new(Substitute.For<IUploadService>(), new WireMockGraphClientFactory(_server));

    [Fact]
    public void when_constructed_then_instance_is_not_null()
    {
        var service = CreateSut();

        _ = service.ShouldNotBeNull();
    }

    [Fact]
    public void when_constructed_then_it_implements_IGraphService()
    {
        var service = CreateSut();

        _ = service.ShouldBeAssignableTo<IGraphService>();
    }

    [Fact]
    public async Task when_get_drive_id_is_called_with_a_pre_cancelled_token_then_operation_is_cancelled()
    {
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.GetDriveIdAsync(AnyAccountId, _ => Task.FromResult(AnyAccessToken), cts.Token));
    }

    [Fact]
    public async Task when_get_root_folders_is_called_with_a_pre_cancelled_token_then_operation_is_cancelled()
    {
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.GetRootFoldersAsync(AnyAccountId, _ => Task.FromResult(AnyAccessToken), cts.Token));
    }

    [Fact]
    public async Task when_get_child_folders_is_called_with_a_pre_cancelled_token_then_operation_is_cancelled()
    {
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.GetChildFoldersAsync(_ => Task.FromResult(AnyAccessToken), new DriveId(AnyDriveId), AnyFolderId, cts.Token));
    }

    [Fact]
    public async Task when_get_quota_is_called_with_a_pre_cancelled_token_then_operation_is_cancelled()
    {
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.GetQuotaAsync(AnyAccountId, _ => Task.FromResult(AnyAccessToken), cts.Token));
    }

    [Fact]
    public async Task when_enumerate_folder_is_called_with_a_pre_cancelled_token_then_operation_is_cancelled()
    {
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.EnumerateFolderAsync(_ => Task.FromResult(AnyAccessToken), new DriveId(AnyDriveId), AnyFolderId, AnyRemotePath, ct: cts.Token));
    }

    [Fact]
    public async Task when_get_folder_id_by_path_is_called_with_a_pre_cancelled_token_then_operation_is_cancelled()
    {
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.GetFolderIdByPathAsync(_ => Task.FromResult(AnyAccessToken), new DriveId(AnyDriveId), AnyRemotePath, cts.Token));
    }

    [Fact]
    public async Task when_get_download_url_is_called_with_a_pre_cancelled_token_then_operation_is_cancelled()
    {
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.GetDownloadUrlAsync(AnyAccountId, _ => Task.FromResult(AnyAccessToken), AnyItemId, cts.Token));
    }

    [Fact]
    public async Task when_upload_file_is_called_with_a_pre_cancelled_token_then_operation_is_cancelled()
    {
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.UploadFileAsync(AnyAccountId, _ => Task.FromResult(AnyAccessToken), AnyLocalPath, AnyRemotePath, AnyFolderId, cts.Token));
    }

    [Fact]
    public async Task when_delete_item_is_called_with_a_pre_cancelled_token_then_operation_is_cancelled()
    {
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.DeleteItemAsync(AnyAccountId, _ => Task.FromResult(AnyAccessToken), AnyItemId, cts.Token));
    }

    [Fact]
    public async Task when_get_root_folders_returns_mixed_items_then_only_folders_are_returned_ordered_by_name()
    {
        SetupDriveContext(AnyDriveId, "root-001");
        _server.Given(Request.Create().WithPath($"/drives/{AnyDriveId}/items/root-001/children").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    value = new object[]
                    {
                        new { id = "f1", name = "ZFolder", folder = new { }, parentReference = new { id = "root-001", driveId = AnyDriveId } },
                        new { id = "f2", name = "AFolder", folder = new { }, parentReference = new { id = "root-001", driveId = AnyDriveId } },
                        new { id = "fi1", name = "readme.txt", file = new { }, parentReference = new { id = "root-001", driveId = AnyDriveId } }
                    }
                }));

        var result = await CreateSut().GetRootFoldersAsync(AnyAccountId, _ => Task.FromResult(AnyAccessToken), TestContext.Current.CancellationToken);

        var folders = result.ShouldBeAssignableTo<Result<List<DriveFolder>, string>.Ok>()!.Value;
        folders.Count.ShouldBe(2);
        folders[0].Name.ShouldBe("AFolder");
        folders[1].Name.ShouldBe("ZFolder");
    }

    [Fact]
    public async Task when_get_folder_id_by_path_receives_a_404_then_null_is_returned()
    {
        _server.Given(Request.Create().UsingAnyMethod())
            .RespondWith(Response.Create()
                .WithStatusCode(404)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { error = new { code = "itemNotFound", message = "The resource could not be found." } }));

        string? result = await CreateSut().GetFolderIdByPathAsync(_ => Task.FromResult(AnyAccessToken), new DriveId(AnyDriveId), AnyRemotePath, TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task when_get_quota_response_has_null_total_then_zero_is_returned_for_both_values()
    {
        SetupDriveContext(AnyDriveId, "root-001");
        _server.Given(Request.Create().WithPath($"/drives/{AnyDriveId}").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { id = AnyDriveId }));

        var quotaResult = await CreateSut().GetQuotaAsync(AnyAccountId, _ => Task.FromResult(AnyAccessToken), TestContext.Current.CancellationToken);

        var (total, used) = quotaResult.ShouldBeAssignableTo<Result<(long Total, long Used), string>.Ok>()!.Value;
        total.ShouldBe(0L);
        used.ShouldBe(0L);
    }

    [Fact]
    public async Task when_get_download_url_item_has_no_additional_data_then_null_is_returned()
    {
        SetupDriveContext(AnyDriveId, "root-001");
        _server.Given(Request.Create().WithPath($"/drives/{AnyDriveId}/items/{AnyItemId}").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { id = AnyItemId }));

        var result = await CreateSut().GetDownloadUrlAsync(AnyAccountId, _ => Task.FromResult(AnyAccessToken), AnyItemId, TestContext.Current.CancellationToken);

        result.ShouldBeAssignableTo<Result<string, string>.Error>();
    }

    [Fact]
    public async Task when_enumerate_folder_contains_subfolders_then_recursive_enumeration_visits_each_child()
    {
        _server.Given(Request.Create().WithPath($"/drives/{AnyDriveId}/items/{AnyFolderId}/children").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    value = new object[]
                    {
                        new { id = "file-root", name = "root-file.txt", file = new { }, size = 100L, parentReference = new { id = AnyFolderId, driveId = AnyDriveId } },
                        new { id = "subfolder-001", name = "SubFolder", folder = new { }, parentReference = new { id = AnyFolderId, driveId = AnyDriveId } }
                    }
                }));
        _server.Given(Request.Create().WithPath($"/drives/{AnyDriveId}/items/subfolder-001/children").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    value = new object[]
                    {
                        new { id = "file-sub", name = "sub-file.txt", file = new { }, size = 200L, parentReference = new { id = "subfolder-001", driveId = AnyDriveId } }
                    }
                }));

        var result = await CreateSut().EnumerateFolderAsync(_ => Task.FromResult(AnyAccessToken), new DriveId(AnyDriveId), AnyFolderId, "root", ct: TestContext.Current.CancellationToken);

        var items = result.ShouldBeAssignableTo<Result<List<DeltaItem>, string>.Ok>()!.Value;
        items.Count.ShouldBe(3);
        items.ShouldContain(i => i.Id.Id == "file-root");
        items.ShouldContain(i => i.Id.Id == "subfolder-001" && i is FolderDeltaItem);
        items.ShouldContain(i => i.Id.Id == "file-sub");
    }

    [Fact]
    public async Task when_enumerate_folder_encounters_a_cycle_via_parentId_then_each_folder_is_visited_exactly_once()
    {
        _server.Given(Request.Create().WithPath($"/drives/{AnyDriveId}/items/{AnyFolderId}/children").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    value = new object[]
                    {
                        new { id = "subfolder-A", name = "SubFolderA", folder = new { }, parentReference = new { id = AnyFolderId, driveId = AnyDriveId } }
                    }
                }));
        _server.Given(Request.Create().WithPath($"/drives/{AnyDriveId}/items/subfolder-A/children").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    value = new object[]
                    {
                        new { id = AnyFolderId, name = "CyclicRef", folder = new { }, parentReference = new { id = "subfolder-A", driveId = AnyDriveId } }
                    }
                }));

        var result = await CreateSut().EnumerateFolderAsync(_ => Task.FromResult(AnyAccessToken), new DriveId(AnyDriveId), AnyFolderId, "root", ct: TestContext.Current.CancellationToken);

        var items = result.ShouldBeAssignableTo<Result<List<DeltaItem>, string>.Ok>()!.Value;
        items.Count.ShouldBe(2);
        items.ShouldContain(i => i.Id.Id == "subfolder-A");
        items.ShouldContain(i => i.Id.Id == AnyFolderId);
    }

    [Fact]
    public async Task when_drive_context_is_resolved_twice_with_the_same_token_then_the_me_drive_endpoint_is_called_only_once()
    {
        SetupDriveContext(AnyDriveId, "root-001");
        var ct = TestContext.Current.CancellationToken;

        var sut = CreateSut();
        _ = await sut.GetDriveIdAsync(AnyAccountId, _ => Task.FromResult(AnyAccessToken), ct);
        _ = await sut.GetDriveIdAsync(AnyAccountId, _ => Task.FromResult(AnyAccessToken), ct);

        (_server.LogEntries?.Count(entry => entry.RequestMessage?.Url?.Contains("/me/drive", StringComparison.OrdinalIgnoreCase) == true) ?? 0).ShouldBe(1);
    }

    [Fact]
    public async Task when_get_drive_id_is_called_and_graph_returns_null_drive_id_then_result_is_error()
    {
        _server.Given(Request.Create().WithPath("/me/drive").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { id = (string?)null }));

        var result = await CreateSut().GetDriveIdAsync(AnyAccountId, _ => Task.FromResult(AnyAccessToken), TestContext.Current.CancellationToken);

        result.ShouldBeAssignableTo<Result<DriveId, string>.Error>();
    }

    [Fact]
    public async Task when_get_drive_id_is_called_and_graph_returns_null_root_item_id_then_result_is_error()
    {
        _server.Given(Request.Create().WithPath("/me/drive").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { id = "drive-001" }));
        _server.Given(Request.Create().WithPath("/drives/drive-001/root").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { id = (string?)null }));

        var result = await CreateSut().GetDriveIdAsync(AnyAccountId, _ => Task.FromResult(AnyAccessToken), TestContext.Current.CancellationToken);

        result.ShouldBeAssignableTo<Result<DriveId, string>.Error>();
    }

    [Fact]
    public async Task when_drive_context_is_resolved_with_same_account_id_but_different_tokens_then_me_drive_endpoint_is_called_only_once()
    {
        const string accountId = "account-001";
        SetupDriveContext(AnyDriveId, "root-001");
        var ct = TestContext.Current.CancellationToken;
        var sut = CreateSut();

        _ = await sut.GetDriveIdAsync(accountId, _ => Task.FromResult(AnyAccessToken), ct);
        _ = await sut.GetDriveIdAsync(accountId, _ => Task.FromResult("refreshed-token"), ct);

        (_server.LogEntries?.Count(entry => entry.RequestMessage?.Url?.Contains("/me/drive", StringComparison.OrdinalIgnoreCase) == true) ?? 0).ShouldBe(1);
    }

    [Fact]
    public async Task when_evict_cached_drive_context_is_called_then_subsequent_call_hits_me_drive_endpoint()
    {
        const string accountId = "account-001";
        SetupDriveContext(AnyDriveId, "root-001");
        var ct = TestContext.Current.CancellationToken;
        var sut = CreateSut();

        _ = await sut.GetDriveIdAsync(accountId, _ => Task.FromResult(AnyAccessToken), ct);
        sut.EvictCachedDriveContext(accountId);
        _ = await sut.GetDriveIdAsync(accountId, _ => Task.FromResult(AnyAccessToken), ct);

        (_server.LogEntries?.Count(entry => entry.RequestMessage?.Url?.Contains("/me/drive", StringComparison.OrdinalIgnoreCase) == true) ?? 0).ShouldBe(2);
    }

    [Fact]
    public async Task when_enumerate_folder_next_link_does_not_pass_guard_then_only_the_first_page_is_returned()
    {
        var nonGraphNextLinkUrl = $"{_server.Url}/drives/{AnyDriveId}/items/{AnyFolderId}/children?$skiptoken=page2";

        _server.Given(Request.Create().WithPath($"/drives/{AnyDriveId}/items/{AnyFolderId}/children").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new Dictionary<string, object>
                {
                    ["value"] = new object[] { new { id = "file-page1", name = "file1.txt", file = new { }, size = 100L, parentReference = new { id = AnyFolderId, driveId = AnyDriveId } } },
                    ["@odata.nextLink"] = nonGraphNextLinkUrl
                }));

        var reportedCounts = new List<int>();

        var result = await CreateSut().EnumerateFolderAsync(_ => Task.FromResult(AnyAccessToken), new DriveId(AnyDriveId), AnyFolderId, AnyRemotePath, onItemDiscovered: count => reportedCounts.Add(count), ct: TestContext.Current.CancellationToken);

        var items = result.ShouldBeAssignableTo<Result<List<DeltaItem>, string>.Ok>()!.Value;
        items.Count.ShouldBe(1);
        reportedCounts.ShouldContain(1);
        reportedCounts.ShouldNotContain(2);
    }

    [Fact]
    public async Task when_get_drive_id_returns_server_error_then_result_is_error()
    {
        _server.Given(Request.Create().UsingAnyMethod())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { error = new { code = "generalException", message = "Server error" } }));

        var result = await CreateSut().GetDriveIdAsync(AnyAccountId, _ => Task.FromResult(AnyAccessToken), TestContext.Current.CancellationToken);

        result.ShouldBeAssignableTo<Result<DriveId, string>.Error>();
    }

    [Fact]
    public async Task when_get_root_folders_returns_server_error_then_result_is_error()
    {
        _server.Given(Request.Create().UsingAnyMethod())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { error = new { code = "generalException", message = "Server error" } }));

        var result = await CreateSut().GetRootFoldersAsync(AnyAccountId, _ => Task.FromResult(AnyAccessToken), TestContext.Current.CancellationToken);

        result.ShouldBeAssignableTo<Result<List<DriveFolder>, string>.Error>();
    }

    [Fact]
    public async Task when_get_quota_returns_server_error_then_result_is_error()
    {
        _server.Given(Request.Create().UsingAnyMethod())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { error = new { code = "generalException", message = "Server error" } }));

        var result = await CreateSut().GetQuotaAsync(AnyAccountId, _ => Task.FromResult(AnyAccessToken), TestContext.Current.CancellationToken);

        result.ShouldBeAssignableTo<Result<(long Total, long Used), string>.Error>();
    }

    [Fact]
    public async Task when_get_folder_id_by_path_returns_server_error_then_null_is_returned()
    {
        _server.Given(Request.Create().UsingAnyMethod())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { error = new { code = "generalException", message = "Server error" } }));

        string? result = await CreateSut().GetFolderIdByPathAsync(_ => Task.FromResult(AnyAccessToken), new DriveId(AnyDriveId), AnyRemotePath, TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task when_get_download_url_returns_server_error_then_result_is_error()
    {
        _server.Given(Request.Create().UsingAnyMethod())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { error = new { code = "generalException", message = "Server error" } }));

        var result = await CreateSut().GetDownloadUrlAsync(AnyAccountId, _ => Task.FromResult(AnyAccessToken), AnyItemId, TestContext.Current.CancellationToken);

        result.ShouldBeAssignableTo<Result<string, string>.Error>();
    }

    [Fact]
    public async Task when_upload_file_returns_server_error_then_result_is_error()
    {
        _server.Given(Request.Create().UsingAnyMethod())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { error = new { code = "generalException", message = "Server error" } }));

        var result = await CreateSut().UploadFileAsync(AnyAccountId, _ => Task.FromResult(AnyAccessToken), AnyLocalPath, AnyRemotePath, AnyFolderId, TestContext.Current.CancellationToken);

        result.ShouldBeAssignableTo<Result<string, string>.Error>();
    }

    [Fact]
    public async Task when_token_factory_throws_then_GetDownloadUrlAsync_returns_error()
    {
        Func<CancellationToken, Task<string>> failingFactory = _ => Task.FromException<string>(new InvalidOperationException("Token acquisition failed."));

        var result = await CreateSut().GetDownloadUrlAsync(AnyAccountId, failingFactory, AnyItemId, TestContext.Current.CancellationToken);

        result.ShouldBeAssignableTo<Result<string, string>.Error>();
    }

    [Fact]
    public async Task when_get_child_folders_returns_server_error_then_result_is_error()
    {
        _server.Given(Request.Create().UsingAnyMethod())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { error = new { code = "generalException", message = "Server error" } }));

        var result = await CreateSut().GetChildFoldersAsync(_ => Task.FromResult(AnyAccessToken), new DriveId(AnyDriveId), AnyFolderId, TestContext.Current.CancellationToken);

        result.ShouldBeAssignableTo<Result<List<DriveFolder>, string>.Error>();
    }

    [Fact]
    public async Task when_enumerate_folder_returns_server_error_then_result_is_error()
    {
        _server.Given(Request.Create().UsingAnyMethod())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { error = new { code = "generalException", message = "Server error" } }));

        var result = await CreateSut().EnumerateFolderAsync(_ => Task.FromResult(AnyAccessToken), new DriveId(AnyDriveId), AnyFolderId, AnyRemotePath, ct: TestContext.Current.CancellationToken);

        result.ShouldBeAssignableTo<Result<List<DeltaItem>, string>.Error>();
    }

    [Fact]
    public async Task when_delete_item_returns_server_error_then_result_is_error()
    {
        SetupDriveContext(AnyDriveId, "root-001");
        _server.Given(Request.Create().WithPath($"/drives/{AnyDriveId}/items/{AnyItemId}").UsingDelete())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { error = new { code = "generalException", message = "Server error" } }));

        var result = await CreateSut().DeleteItemAsync(AnyAccountId, _ => Task.FromResult(AnyAccessToken), AnyItemId, TestContext.Current.CancellationToken);

        result.ShouldBeAssignableTo<Result<System.Reactive.Unit, string>.Error>();
    }

    private void SetupDriveContext(string driveId, string rootId)
    {
        _server.Given(Request.Create().WithPath("/me/drive").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { id = driveId }));
        _server.Given(Request.Create().WithPath($"/drives/{driveId}/root").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { id = rootId }));
    }
}
