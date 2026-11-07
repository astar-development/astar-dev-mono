using AStarDev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Graph;

public sealed class GivenAGraphServiceCachingTheDriveContext : IDisposable
{
    private const string AnyAccountId = "account-001";
    private const string AnyAccessToken = "any-access-token";
    private const string AnyDriveId = "drive-001";
    private const string AnyRootId = "root-001";
    private const string MeDrivePath = "/me/drive";

    private readonly GraphServiceTestHosts hosts = new();
    private readonly WireMockServer server;

    public GivenAGraphServiceCachingTheDriveContext() => server = hosts.Server;

    public void Dispose() => hosts.Dispose();

    private IGraphService CreateSut() => hosts.CreateHost().GraphService;

    private static Task<string> Token(CancellationToken cancellationToken) => Task.FromResult(AnyAccessToken);

    private int MeDriveCallCount() => server.LogEntries?.Count(entry => entry.RequestMessage?.Url?.Contains(MeDrivePath, StringComparison.OrdinalIgnoreCase) == true) ?? 0;

    private void SetupDriveContext(string driveId, string? rootId)
    {
        server.Given(Request.Create().WithPath(MeDrivePath).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { id = driveId }));
        server.Given(Request.Create().WithPath($"/drives/{driveId}/root").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { id = rootId }));
    }

    [Fact]
    public async Task when_the_drive_context_resolves_then_the_drive_id_is_returned()
    {
        SetupDriveContext(AnyDriveId, AnyRootId);

        var result = await CreateSut().GetDriveIdAsync(AnyAccountId, Token, TestContext.Current.CancellationToken);

        var ok = result.ShouldBeAssignableTo<Ok<DriveId, string>>();
        ok.Value.Value.ShouldBe(AnyDriveId);
    }

    [Fact]
    public async Task when_root_folders_are_requested_then_the_children_of_the_resolved_root_item_are_read()
    {
        SetupDriveContext(AnyDriveId, AnyRootId);
        server.Given(Request.Create().WithPath($"/drives/{AnyDriveId}/items/{AnyRootId}/children").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { value = new object[] { new { id = "folder-a", name = "Alpha", folder = new { childCount = 0 } } } }));

        var result = await CreateSut().GetRootFoldersAsync(AnyAccountId, Token, TestContext.Current.CancellationToken);

        var ok = result.ShouldBeAssignableTo<Ok<List<DriveFolder>, string>>();
        ok.Value.ShouldHaveSingleItem().Name.ShouldBe("Alpha");
    }

    [Fact]
    public async Task when_the_drive_id_is_requested_twice_for_the_same_account_then_the_me_drive_endpoint_is_only_called_once()
    {
        SetupDriveContext(AnyDriveId, AnyRootId);
        var cancellationToken = TestContext.Current.CancellationToken;
        var sut = CreateSut();

        _ = await sut.GetDriveIdAsync(AnyAccountId, Token, cancellationToken);
        _ = await sut.GetDriveIdAsync(AnyAccountId, Token, cancellationToken);

        MeDriveCallCount().ShouldBe(1);
    }

    [Fact]
    public async Task when_the_cached_drive_context_is_evicted_then_the_next_request_calls_the_me_drive_endpoint_again()
    {
        SetupDriveContext(AnyDriveId, AnyRootId);
        var cancellationToken = TestContext.Current.CancellationToken;
        var sut = CreateSut();

        _ = await sut.GetDriveIdAsync(AnyAccountId, Token, cancellationToken);
        sut.EvictCachedDriveContext(AnyAccountId);
        _ = await sut.GetDriveIdAsync(AnyAccountId, Token, cancellationToken);

        MeDriveCallCount().ShouldBe(2);
    }

    [Fact]
    public void when_the_drive_context_of_an_unknown_account_is_evicted_then_no_exception_is_thrown()
    {
        var sut = CreateSut();

        var exception = Record.Exception(() => sut.EvictCachedDriveContext("unknown-account"));

        exception.ShouldBeNull();
    }

    [Fact]
    public async Task when_graph_returns_a_null_drive_id_then_the_result_is_a_failure()
    {
        server.Given(Request.Create().WithPath(MeDrivePath).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { id = (string?)null }));

        var result = await CreateSut().GetDriveIdAsync(AnyAccountId, Token, TestContext.Current.CancellationToken);

        result.ShouldBeAssignableTo<Fail<DriveId, string>>().Error.ShouldBe("Could not retrieve drive ID.");
    }

    [Fact]
    public async Task when_graph_returns_a_null_root_item_id_then_the_result_is_a_failure()
    {
        SetupDriveContext(AnyDriveId, rootId: null);

        var result = await CreateSut().GetDriveIdAsync(AnyAccountId, Token, TestContext.Current.CancellationToken);

        result.ShouldBeAssignableTo<Fail<DriveId, string>>().Error.ShouldBe("Could not retrieve root item ID.");
    }

    [Fact]
    public async Task when_the_drive_id_is_requested_with_a_pre_cancelled_token_then_the_operation_is_cancelled()
    {
        var sut = CreateSut();
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        _ = await Should.ThrowAsync<OperationCanceledException>(() => sut.GetDriveIdAsync(AnyAccountId, Token, cancellationTokenSource.Token));
    }
}
