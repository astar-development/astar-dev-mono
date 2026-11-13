using System.Net;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.OneDrive.TestsUnit;

public sealed class GivenAnOneDriveFolderCreator : IDisposable
{
    private readonly OneDriveTransferFixture fixture = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static Option<RemoteItemId> Parent(string id) => RemoteItemIdFactory.Create(id).Match(Option.Some, _ => Option.None<RemoteItemId>());

    private static string FolderJson(string id, string name, string parent) => "{\"id\":\"" + id + "\",\"name\":\"" + name + "\",\"parentReference\":{\"id\":\"" + parent + "\"},\"folder\":{\"childCount\":0}}";

    private static string Describe(Result<RemoteFolder, ProviderError> result) => result.Match(folder => $"ok {folder.Id.Value} {folder.Name} {folder.ParentId.Value}", error => error.GetType().Name);

    [Fact]
    public async Task when_a_folder_is_created_then_the_post_asks_to_fail_on_a_name_clash_and_the_new_folder_is_returned()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        fixture.Graph.Respond = _ => RecordingGraphHandler.Json(HttpStatusCode.Created, FolderJson("f1", "New", "p1"));

        var result = await fixture.Folders.EnsureFolderAsync(fixture.Account, Parent("p1"), "New", CancellationToken);

        Describe(result).ShouldBe("ok f1 New p1");
        var post = fixture.Graph.Requests.ShouldHaveSingleItem();
        post.Method.ShouldBe(HttpMethod.Post);
        post.Url.AbsolutePath.ShouldBe($"{OneDriveTransferFixture.Items}p1/children");
        post.Body.ShouldContain("\"@microsoft.graph.conflictBehavior\":\"fail\"");
        post.Body.ShouldContain("\"folder\":{}");
        post.Authorization.ShouldBe($"Bearer {OneDriveTransferFixture.Token}");
    }

    [Fact]
    public async Task when_a_folder_is_created_in_the_drive_root_then_the_root_alias_is_used()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        fixture.Graph.Respond = _ => RecordingGraphHandler.Json(HttpStatusCode.Created, FolderJson("f1", "New", "root-id"));

        _ = await fixture.Folders.EnsureFolderAsync(fixture.Account, Option.None<RemoteItemId>(), "New", CancellationToken);

        fixture.Graph.Requests.ShouldHaveSingleItem().Url.AbsolutePath.ShouldBe($"{OneDriveTransferFixture.Items}root/children");
    }

    [Theory]
    [InlineData(409)]
    [InlineData(412)]
    public async Task when_the_folder_already_exists_then_it_is_looked_up_by_name_and_returned_not_failed(int status)
    {
        _ = await fixture.BuildAsync(CancellationToken);
        fixture.Graph.Respond = request => request.Method == HttpMethod.Post
            ? RecordingGraphHandler.Json((HttpStatusCode)status, "{\"error\":{\"code\":\"nameAlreadyExists\"}}")
            : RecordingGraphHandler.Json(HttpStatusCode.OK, FolderJson("existing", "New", "p1"));

        var result = await fixture.Folders.EnsureFolderAsync(fixture.Account, Parent("p1"), "New", CancellationToken);

        Describe(result).ShouldBe("ok existing New p1");
        fixture.Graph.Requests[1].Method.ShouldBe(HttpMethod.Get);
        fixture.Graph.Requests[1].Url.AbsolutePath.ShouldBe($"{OneDriveTransferFixture.Items}p1:/New");
    }

    [Fact]
    public async Task when_the_name_is_held_by_a_file_then_the_lookup_reports_a_name_conflict()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        fixture.Graph.Respond = request => request.Method == HttpMethod.Post
            ? RecordingGraphHandler.Json(HttpStatusCode.Conflict, "{}")
            : RecordingGraphHandler.Json(HttpStatusCode.OK, "{\"id\":\"file-1\",\"name\":\"New\",\"file\":{}}");

        var result = await fixture.Folders.EnsureFolderAsync(fixture.Account, Parent("p1"), "New", CancellationToken);

        Describe(result).ShouldBe("NameConflict");
    }

    [Theory]
    [InlineData(500, "Transient")]
    [InlineData(404, "NotFound")]
    [InlineData(429, "Throttled")]
    public async Task when_the_lookup_after_a_name_clash_fails_then_the_failure_is_returned_and_never_treated_as_missing(int lookupStatus, string expected)
    {
        _ = await fixture.BuildAsync(CancellationToken);
        fixture.Graph.Respond = request => request.Method == HttpMethod.Post
            ? RecordingGraphHandler.Json(HttpStatusCode.Conflict, "{}")
            : RecordingGraphHandler.Json((HttpStatusCode)lookupStatus, "{}");

        var result = await fixture.Folders.EnsureFolderAsync(fixture.Account, Parent("p1"), "New", CancellationToken);

        Describe(result).ShouldBe(expected);
    }

    [Fact]
    public async Task when_the_same_folder_is_ensured_twice_at_once_then_one_creates_it_and_the_other_finds_it_and_both_get_the_same_folder()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        var posts = 0;
        fixture.Graph.Respond = request => request.Method == HttpMethod.Post
            ? (Interlocked.Increment(ref posts) == 1 ? RecordingGraphHandler.Json(HttpStatusCode.Created, FolderJson("same", "New", "p1")) : RecordingGraphHandler.Json(HttpStatusCode.Conflict, "{}"))
            : RecordingGraphHandler.Json(HttpStatusCode.OK, FolderJson("same", "New", "p1"));

        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => fixture.Folders.EnsureFolderAsync(fixture.Account, Parent("p1"), "New", CancellationToken), CancellationToken)));

        results.Select(Describe).Distinct().ShouldBe(["ok same New p1"]);
    }

    [Fact]
    public async Task when_the_provider_rejects_the_sign_in_then_the_creator_reports_authentication_required()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        fixture.Graph.Respond = _ => RecordingGraphHandler.Json(HttpStatusCode.Unauthorized, "{}");

        var result = await fixture.Folders.EnsureFolderAsync(fixture.Account, Parent("p1"), "New", CancellationToken);

        Describe(result).ShouldBe("AuthenticationRequired");
    }

    [Fact]
    public async Task when_a_folder_name_has_special_characters_then_the_lookup_path_is_escaped_and_the_log_has_no_name()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        fixture.Graph.Respond = request => request.Method == HttpMethod.Post
            ? RecordingGraphHandler.Json(HttpStatusCode.Conflict, "{}")
            : RecordingGraphHandler.Json(HttpStatusCode.OK, FolderJson("x", "Mi Carpeta #1", "p1"));

        _ = await fixture.Folders.EnsureFolderAsync(fixture.Account, Parent("p1"), "Mi Carpeta #1", CancellationToken);

        fixture.Graph.Requests[1].Url.AbsoluteUri.ShouldEndWith(":/Mi%20Carpeta%20%231");
        string.Join('\n', fixture.Logs.Logger.Messages).ShouldNotContain("Carpeta");
    }

    public void Dispose() => fixture.Dispose();
}
