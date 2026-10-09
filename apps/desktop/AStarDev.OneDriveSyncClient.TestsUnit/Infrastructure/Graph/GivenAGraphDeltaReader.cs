using System.Text.Json.Nodes;
using AStarDev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Graph;

public sealed class GivenAGraphDeltaReader : IDisposable
{
    private const string DeltaPath = "/drives/drive-001/root/delta";
    private const string GraphDeltaBase = "https://graph.microsoft.com/v1.0/drives/drive-001/root/delta";

    private readonly WireMockServer server = WireMockServer.Start();
    private readonly DriveId driveId = new("drive-001");
    private bool disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposed)
            return;

        disposed = true;

        if (disposing)
            server.Stop();
    }

    private GraphDeltaReader CreateSut() => new(new WireMockGraphClientFactory(server));

    private static Task<string> Token(CancellationToken cancellationToken) => Task.FromResult("any-access-token");

    private void StubDelta(string token, JsonObject body, int statusCode = 200)
        => server.Given(Request.Create().WithPath(DeltaPath).WithParam("token", token).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(statusCode).WithHeader("Content-Type", "application/json").WithBody(body.ToJsonString()));

    private static JsonObject FileNode(string id, string parentId)
        => new() { ["id"] = id, ["name"] = id, ["file"] = new JsonObject(), ["parentReference"] = new JsonObject { ["id"] = parentId } };

    [Fact]
    public async Task when_latest_delta_link_is_requested_then_the_delta_link_from_graph_is_returned()
    {
        StubDelta("latest", new JsonObject { ["@odata.deltaLink"] = $"{GraphDeltaBase}?token=abc123", ["value"] = new JsonArray() });
        var sut = CreateSut();

        var result = await sut.GetLatestDeltaLinkAsync(Token, driveId, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Ok<string, string>>().Value.ShouldBe($"{GraphDeltaBase}?token=abc123");
    }

    [Fact]
    public async Task when_latest_delta_link_is_requested_and_graph_fails_then_a_failure_is_returned()
    {
        StubDelta("latest", [], 500);
        var sut = CreateSut();

        var result = await sut.GetLatestDeltaLinkAsync(Token, driveId, TestContext.Current.CancellationToken);

        _ = result.ShouldBeOfType<Fail<string, string>>();
    }

    [Fact]
    public async Task when_changes_are_requested_and_nothing_changed_then_no_changes_and_the_new_delta_link_are_returned()
    {
        StubDelta("old", new JsonObject { ["@odata.deltaLink"] = $"{GraphDeltaBase}?token=new", ["value"] = new JsonArray() });
        var sut = CreateSut();

        var result = await sut.GetChangesAsync(Token, driveId, $"{GraphDeltaBase}?token=old", TestContext.Current.CancellationToken);

        var found = result.ShouldBeOfType<Ok<DeltaQueryResult, string>>().Value.ShouldBeOfType<DeltaChangesFound>();
        found.Changes.ShouldBeEmpty();
        found.NextDeltaLink.ShouldBe($"{GraphDeltaBase}?token=new");
    }

    [Fact]
    public async Task when_changes_are_requested_then_changed_and_deleted_items_are_mapped()
    {
        StubDelta("old", new JsonObject
        {
            ["@odata.deltaLink"] = $"{GraphDeltaBase}?token=new",
            ["value"] = new JsonArray(FileNode("file-1", "folder-1"), new JsonObject { ["id"] = "gone-1", ["deleted"] = new JsonObject() })
        });
        var sut = CreateSut();

        var result = await sut.GetChangesAsync(Token, driveId, $"{GraphDeltaBase}?token=old", TestContext.Current.CancellationToken);

        var found = result.ShouldBeOfType<Ok<DeltaQueryResult, string>>().Value.ShouldBeOfType<DeltaChangesFound>();
        found.Changes.Count.ShouldBe(2);
        found.Changes[0].ShouldBeOfType<ItemChanged>().ParentId.ShouldBe(Option.Some("folder-1"));
        found.Changes[1].ShouldBeOfType<ItemDeleted>().ItemId.ShouldBe("gone-1");
    }

    [Fact]
    public async Task when_changes_span_multiple_pages_then_all_pages_are_read_and_the_final_delta_link_is_returned()
    {
        StubDelta("old", new JsonObject { ["@odata.nextLink"] = $"{GraphDeltaBase}?token=page2", ["value"] = new JsonArray(FileNode("file-1", "folder-1")) });
        StubDelta("page2", new JsonObject { ["@odata.deltaLink"] = $"{GraphDeltaBase}?token=new", ["value"] = new JsonArray(FileNode("file-2", "folder-1")) });
        var sut = CreateSut();

        var result = await sut.GetChangesAsync(Token, driveId, $"{GraphDeltaBase}?token=old", TestContext.Current.CancellationToken);

        var found = result.ShouldBeOfType<Ok<DeltaQueryResult, string>>().Value.ShouldBeOfType<DeltaChangesFound>();
        found.Changes.Select(change => change.ItemId).ShouldBe(["file-1", "file-2"]);
        found.NextDeltaLink.ShouldBe($"{GraphDeltaBase}?token=new");
    }

    [Fact]
    public async Task when_graph_reports_the_token_has_expired_then_resync_is_required()
    {
        StubDelta("old", new JsonObject { ["error"] = new JsonObject { ["code"] = "resyncRequired", ["message"] = "gone" } }, 410);
        var sut = CreateSut();

        var result = await sut.GetChangesAsync(Token, driveId, $"{GraphDeltaBase}?token=old", TestContext.Current.CancellationToken);

        _ = result.ShouldBeOfType<Ok<DeltaQueryResult, string>>().Value.ShouldBeOfType<DeltaResyncRequired>();
    }

    [Fact]
    public async Task when_the_stored_delta_link_has_no_token_then_resync_is_required()
    {
        var sut = CreateSut();

        var result = await sut.GetChangesAsync(Token, driveId, GraphDeltaBase, TestContext.Current.CancellationToken);

        _ = result.ShouldBeOfType<Ok<DeltaQueryResult, string>>().Value.ShouldBeOfType<DeltaResyncRequired>();
    }

    [Fact]
    public async Task when_graph_fails_with_a_server_error_then_a_failure_is_returned()
    {
        StubDelta("old", [], 500);
        var sut = CreateSut();

        var result = await sut.GetChangesAsync(Token, driveId, $"{GraphDeltaBase}?token=old", TestContext.Current.CancellationToken);

        _ = result.ShouldBeOfType<Fail<DeltaQueryResult, string>>();
    }
}
