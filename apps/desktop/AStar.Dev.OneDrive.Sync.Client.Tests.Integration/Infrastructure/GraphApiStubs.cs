using System.Text.Json.Nodes;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Integration.Infrastructure;

internal static class GraphApiStubs
{
    private const string DeltaPath = "/v1.0/me/drive/root/delta";
    private const string TokenPath = "/oauth2/v2.0/token";
    private const string FakeDeltaLink = "https://graph.microsoft.com/v1.0/me/drive/root/delta?$deltatoken=test-token";
    private const string ContentTypeJson = "application/json";

    internal sealed record DriveItemStub(string Id, string Name, long Size, bool IsFolder, string ParentId, string DriveId);

    /// <summary>Stubs GET /v1.0/me/drive/root/delta to return a page containing the supplied items.</summary>
    public static void StubDriveItemsPage(WireMockServer server, IEnumerable<DriveItemStub> items)
    {
        var valueArray = new JsonArray();
        foreach(var stub in items)
            valueArray.Add(BuildDriveItemNode(stub));

        var body = new JsonObject
        {
            ["@odata.deltaLink"] = FakeDeltaLink,
            ["value"] = valueArray
        }.ToJsonString();

        server
            .Given(Request.Create().WithPath(DeltaPath).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(body).WithHeader("Content-Type", ContentTypeJson));
    }

    /// <summary>Stubs GET /v1.0/me/drive/root/delta to return an empty value array with a deltaLink (sync already up to date).</summary>
    public static void StubEmptyDelta(WireMockServer server)
    {
        var body = new JsonObject
        {
            ["@odata.deltaLink"] = FakeDeltaLink,
            ["value"] = new JsonArray()
        }.ToJsonString();

        server
            .Given(Request.Create().WithPath(DeltaPath).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(body).WithHeader("Content-Type", ContentTypeJson));
    }

    /// <summary>Stubs POST /oauth2/v2.0/token to return a fake bearer token.</summary>
    public static void StubAuthToken(WireMockServer server)
    {
        var body = new JsonObject
        {
            ["token_type"] = "Bearer",
            ["access_token"] = "fake-access-token-for-testing",
            ["expires_in"] = 3600
        }.ToJsonString();

        server
            .Given(Request.Create().WithPath(TokenPath).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(body).WithHeader("Content-Type", ContentTypeJson));
    }

    private static JsonObject BuildDriveItemNode(DriveItemStub stub)
    {
        var node = new JsonObject
        {
            ["id"] = stub.Id,
            ["name"] = stub.Name,
            ["parentReference"] = new JsonObject
            {
                ["id"] = stub.ParentId,
                ["driveId"] = stub.DriveId
            }
        };

        if(stub.IsFolder)
            node["folder"] = new JsonObject();
        else
        {
            node["file"] = new JsonObject();
            node["size"] = stub.Size;
        }

        return node;
    }
}
