using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Util;
using WireMock.Types;
using WireMockBodyType = WireMock.Types.BodyType;
using WireMockRequest = WireMock.RequestBuilders.Request;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using AStar.Dev.OneDrive.Sync.Client.Tests.Unit.TestHelpers;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync.Jobs;

public sealed class GivenAnUploadServiceLogging
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

    [Fact]
    public async Task when_upload_succeeds_then_completed_log_event_2802_is_emitted()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile(LocalFilePath).Which(m => m.HasBytesContent(new byte[64]));
        using var server = WireMockServer.Start();

        server.Given(WireMockRequest.Create().UsingPost())
              .RespondWith(Response.Create().WithStatusCode(200).WithBody(SessionJson(server)).WithHeader("Content-Type", "application/json"));

        server.Given(WireMockRequest.Create().UsingPut().WithPath("/chunk-upload"))
              .RespondWith(Response.Create().WithCallback(_ => Created201Response()));

        var logger = new TestLogger<UploadService>();
        var sut = new UploadService(CreateChunkClientFactory(), mockFileSystem, logger);

        await sut.UploadAsync(BuildGraphClient(server), new DriveId(DriveIdValue), ParentFolderId, LocalFilePath, RemotePath, ct: TestContext.Current.CancellationToken);

        logger.Entries.ShouldContain(e => e.Level == LogLevel.Information && e.EventId.Id == 2802);
    }

    [Fact]
    public async Task when_upload_fails_then_completed_log_event_2802_is_not_emitted()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile(LocalFilePath).Which(m => m.HasBytesContent(new byte[64]));
        using var server = WireMockServer.Start();

        server.Given(WireMockRequest.Create().UsingPost())
              .RespondWith(Response.Create().WithStatusCode(200).WithBody(SessionJson(server)).WithHeader("Content-Type", "application/json"));

        server.Given(WireMockRequest.Create().UsingPut().WithPath("/chunk-upload"))
              .RespondWith(Response.Create().WithStatusCode(201).WithHeader("Content-Type", "application/json").WithBody("{}"));

        var logger = new TestLogger<UploadService>();
        var sut = new UploadService(CreateChunkClientFactory(), mockFileSystem, logger);

        await sut.UploadAsync(BuildGraphClient(server), new DriveId(DriveIdValue), ParentFolderId, LocalFilePath, RemotePath, ct: TestContext.Current.CancellationToken);

        logger.Entries.ShouldNotContain(e => e.EventId.Id == 2802);
    }

    private static WireMock.ResponseMessage Created201Response()
    {
        var msg = new WireMock.ResponseMessage
        {
            StatusCode = 201,
            Headers = new Dictionary<string, WireMockList<string>> { { "Content-Type", new WireMockList<string>("application/json") } },
            BodyData = new BodyData { DetectedBodyType = WireMockBodyType.String, BodyAsString = ItemIdJson() }
        };

        return msg;
    }
}
