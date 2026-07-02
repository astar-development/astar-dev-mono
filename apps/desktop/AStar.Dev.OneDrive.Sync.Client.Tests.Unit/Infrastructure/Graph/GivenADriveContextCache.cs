using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Graph;

public sealed class GivenADriveContextCache : IDisposable
{
    private const string AnyAccountId = "account-001";
    private const string AnyAccessToken = "any-access-token";
    private const string AnyDriveId = "drive-001";
    private const string AnyRootId = "root-001";

    private readonly WireMockServer server = WireMockServer.Start();

    public void Dispose() => server.Stop();

    private DriveContextCache CreateSut() => new(new WireMockGraphClientFactory(server));

    private void SetupDriveContext(string driveId, string rootId)
    {
        server.Given(Request.Create().WithPath("/me/drive").UsingGet())
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
    public void when_constructed_then_instance_is_not_null()
    {
        var sut = CreateSut();

        _ = sut.ShouldNotBeNull();
    }

    [Fact]
    public async Task when_resolved_successfully_then_result_is_ok_with_correct_drive_id()
    {
        SetupDriveContext(AnyDriveId, AnyRootId);

        var result = await CreateSut().ResolveAsync(AnyAccountId, _ => Task.FromResult(AnyAccessToken), TestContext.Current.CancellationToken);

        var ok = result.ShouldBeAssignableTo<Result<(Microsoft.Graph.GraphServiceClient Client, DriveContext Ctx), string>.Ok>()!;
        ok.Value.Ctx.DriveId.Value.ShouldBe(AnyDriveId);
        ok.Value.Ctx.RootId.ShouldBe(AnyRootId);
    }

    [Fact]
    public async Task when_resolved_twice_for_same_account_then_me_drive_endpoint_is_only_called_once()
    {
        SetupDriveContext(AnyDriveId, AnyRootId);
        var ct = TestContext.Current.CancellationToken;
        var sut = CreateSut();

        _ = await sut.ResolveAsync(AnyAccountId, _ => Task.FromResult(AnyAccessToken), ct);
        _ = await sut.ResolveAsync(AnyAccountId, _ => Task.FromResult(AnyAccessToken), ct);

        (server.LogEntries?.Count(entry => entry.RequestMessage?.Url?.Contains("/me/drive", StringComparison.OrdinalIgnoreCase) == true) ?? 0).ShouldBe(1);
    }

    [Fact]
    public async Task when_evict_is_called_then_subsequent_resolve_hits_me_drive_endpoint_again()
    {
        SetupDriveContext(AnyDriveId, AnyRootId);
        var ct = TestContext.Current.CancellationToken;
        var sut = CreateSut();

        _ = await sut.ResolveAsync(AnyAccountId, _ => Task.FromResult(AnyAccessToken), ct);
        sut.Evict(AnyAccountId);
        _ = await sut.ResolveAsync(AnyAccountId, _ => Task.FromResult(AnyAccessToken), ct);

        (server.LogEntries?.Count(entry => entry.RequestMessage?.Url?.Contains("/me/drive", StringComparison.OrdinalIgnoreCase) == true) ?? 0).ShouldBe(2);
    }

    [Fact]
    public void when_evict_is_called_for_unknown_account_then_no_exception_is_thrown()
    {
        var sut = CreateSut();

        var exception = Record.Exception(() => sut.Evict("unknown-account"));

        exception.ShouldBeNull();
    }

    [Fact]
    public async Task when_graph_returns_null_drive_id_then_result_is_error()
    {
        server.Given(Request.Create().WithPath("/me/drive").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { id = (string?)null }));

        var result = await CreateSut().ResolveAsync(AnyAccountId, _ => Task.FromResult(AnyAccessToken), TestContext.Current.CancellationToken);

        result.ShouldBeAssignableTo<Result<(Microsoft.Graph.GraphServiceClient Client, DriveContext Ctx), string>.Error>();
    }

    [Fact]
    public async Task when_graph_returns_null_root_item_id_then_result_is_error()
    {
        server.Given(Request.Create().WithPath("/me/drive").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { id = AnyDriveId }));
        server.Given(Request.Create().WithPath($"/drives/{AnyDriveId}/root").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { id = (string?)null }));

        var result = await CreateSut().ResolveAsync(AnyAccountId, _ => Task.FromResult(AnyAccessToken), TestContext.Current.CancellationToken);

        result.ShouldBeAssignableTo<Result<(Microsoft.Graph.GraphServiceClient Client, DriveContext Ctx), string>.Error>();
    }

    [Fact]
    public async Task when_resolve_is_called_with_a_pre_cancelled_token_then_operation_is_cancelled()
    {
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.ResolveAsync(AnyAccountId, _ => Task.FromResult(AnyAccessToken), cts.Token));
    }
}
