using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using WireMock;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Util;
using WireMock.Types;
using WireMockBodyType = WireMock.Types.BodyType;
using WireMockRequest = WireMock.RequestBuilders.Request;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using AStar.Dev.Functional.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Integration.Sync.Jobs;

public sealed class GivenAnUploadService
{
    private const string DriveIdValue   = "drive-001";
    private const string ParentFolderId = "folder-001";
    private const string RemotePath     = "test-file.bin";
    private const string ExpectedItemId = "item-abc-123";
    private const string LocalFilePath  = "/mock/test-file.bin";

    private static IHttpClientFactory CreateChunkClientFactory()
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(new HttpClient());

        return factory;
    }

    private static GraphServiceClient BuildGraphClient(WireMockServer server) =>
        new(new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: new HttpClient { BaseAddress = new Uri(server.Urls[0]) }));

    private static string SessionJson(WireMockServer server) =>
        $"{{\"uploadUrl\":\"{server.Urls[0]}/chunk-upload\"}}";

    private static string ItemIdJson() =>
        $"{{\"id\":\"{ExpectedItemId}\"}}";

    private static GraphServiceClient BuildAnonymousGraphClient() =>
        new(new HttpClientRequestAdapter(new AnonymousAuthenticationProvider()));

    [Fact]
    public async Task when_upload_chunk_is_rate_limited_beyond_max_retries_then_result_is_error()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile(LocalFilePath).Which(m => m.HasBytesContent(new byte[64]));
        using var server = WireMockServer.Start();

        server.Given(WireMockRequest.Create().UsingPost())
              .RespondWith(Response.Create().WithStatusCode(200).WithBody(SessionJson(server)).WithHeader("Content-Type", "application/json"));

        server.Given(WireMockRequest.Create().UsingPut().WithPath("/chunk-upload"))
              .RespondWith(Response.Create().WithStatusCode(429));

        var timeProvider = new FakeTimeProvider();
        var sut = new UploadService(CreateChunkClientFactory(), mockFileSystem, Substitute.For<ILogger<UploadService>>(), timeProvider);

        var uploadTask = sut.UploadAsync(BuildGraphClient(server), new DriveId(DriveIdValue), ParentFolderId, LocalFilePath, RemotePath, ct: TestContext.Current.CancellationToken);

        while (!uploadTask.IsCompleted)
        {
            await Task.Delay(1, TestContext.Current.CancellationToken);
            timeProvider.Advance(TimeSpan.FromMinutes(5));
        }

        var result = await uploadTask;

        result.ShouldBeAssignableTo<Result<string, string>.Error>();
    }

    private static ResponseMessage Accepted202Response() =>
        new() { StatusCode = 202, Headers = JsonContentTypeHeaders() };

    private static ResponseMessage Created201Response()
    {
        var msg = new ResponseMessage
        {
            StatusCode = 201,
            Headers = JsonContentTypeHeaders(),
            BodyData = new BodyData { DetectedBodyType = WireMockBodyType.String, BodyAsString = ItemIdJson() }
        };

        return msg;
    }

    private static ResponseMessage Ok200Response()
    {
        var msg = new ResponseMessage
        {
            StatusCode = 200,
            Headers = JsonContentTypeHeaders(),
            BodyData = new BodyData { DetectedBodyType = WireMockBodyType.String, BodyAsString = ItemIdJson() }
        };

        return msg;
    }

    private static Dictionary<string, WireMockList<string>> JsonContentTypeHeaders() =>
        new() { { "Content-Type", new WireMockList<string>("application/json") } };
}
