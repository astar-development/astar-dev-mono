using AStarDev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Graph;

public sealed class GivenAGraphServiceFollowingNextLinks : IDisposable
{
    private const string AnyAccessToken = "any-access-token";
    private const string AnyDriveId = "drive-001";
    private const string AnyFolderId = "folder-001";
    private const string ChildrenPath = $"/drives/{AnyDriveId}/items/{AnyFolderId}/children";
    private const string SecondPagePath = $"/v1.0{ChildrenPath}";

    private readonly GraphServiceTestHosts hosts = new();
    private readonly WireMockServer server;

    public GivenAGraphServiceFollowingNextLinks() => server = hosts.Server;

    public void Dispose() => hosts.Dispose();

    private static Task<string> Token(CancellationToken cancellationToken) => Task.FromResult(AnyAccessToken);

    private static object Folder(string id, string name) => new { id, name, folder = new { }, parentReference = new { id = AnyFolderId, driveId = AnyDriveId } };

    private void StubTwoPages(string nextLink)
    {
        server.Given(Request.Create().WithPath(ChildrenPath).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new Dictionary<string, object> { ["value"] = new[] { Folder("f1", "FirstPage") }, ["@odata.nextLink"] = nextLink }));
        server.Given(Request.Create().WithPath(SecondPagePath).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new Dictionary<string, object> { ["value"] = new[] { Folder("f2", "SecondPage") } }));
    }

    private async Task<List<string>> ChildFolderNamesAsync()
    {
        var result = await hosts.CreateHost().GraphService.GetChildFoldersAsync(Token, new DriveId(AnyDriveId), AnyFolderId, TestContext.Current.CancellationToken);

        return [.. result.ShouldBeAssignableTo<Ok<List<DriveFolder>, string>>().Value.Select(folder => folder.Name)];
    }

    [Theory]
    [InlineData("https://graph.microsoft.com/v1.0/drives/drive-001/items/folder-001/children?$skiptoken=page2")]
    [InlineData("https://Graph.Microsoft.Com/v1.0/drives/drive-001/items/folder-001/children?$skiptoken=page2")]
    public async Task when_the_next_link_is_an_https_graph_url_then_the_next_page_is_followed(string nextLink)
    {
        StubTwoPages(nextLink);

        var names = await ChildFolderNamesAsync();

        names.ShouldBe(["FirstPage", "SecondPage"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a valid uri !!!")]
    [InlineData("http://graph.microsoft.com/v1.0/drives/drive-001/items/folder-001/children?$skiptoken=page2")]
    [InlineData("https://evil.example.com/v1.0/drives/drive-001/items/folder-001/children?$skiptoken=page2")]
    [InlineData("https://graph.microsoft.com.evil.example.com/v1.0/drives/drive-001/items/folder-001/children?$skiptoken=page2")]
    public async Task when_the_next_link_is_not_an_https_graph_url_then_only_the_first_page_is_returned(string nextLink)
    {
        StubTwoPages(nextLink);

        var names = await ChildFolderNamesAsync();

        names.ShouldBe(["FirstPage"]);
        server.LogEntries.ShouldNotContain(entry => entry.RequestMessage != null && entry.RequestMessage.Path == SecondPagePath);
    }
}
