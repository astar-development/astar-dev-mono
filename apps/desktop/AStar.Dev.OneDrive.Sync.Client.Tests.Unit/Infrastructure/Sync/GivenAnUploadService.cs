using System.Net.Http;
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
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class GivenAnUploadService
{
    private const string DriveId = "drive-001";
    private const string ParentFolderId = "folder-001";
    private const string RemotePath = "test-file.bin";
    private const string ExpectedItemId = "item-abc-123";

    private static IHttpClientFactory CreateChunkClientFactory(WireMockServer server)
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
    public void when_constructed_then_service_implements_IUploadService() =>
        new UploadService(Substitute.For<IHttpClientFactory>()).ShouldBeAssignableTo<IUploadService>();

    private static GraphServiceClient BuildAnonymousGraphClient() =>
        new(new HttpClientRequestAdapter(new AnonymousAuthenticationProvider()));

    [Fact]
    public async Task when_upload_async_is_called_with_nonexistent_local_path_then_FileNotFoundException_is_thrown()
    {
        var sut = new UploadService(Substitute.For<IHttpClientFactory>());

        await Should.ThrowAsync<FileNotFoundException>(() =>
            sut.UploadAsync(BuildAnonymousGraphClient(), DriveId, ParentFolderId, "/nonexistent/path/file.bin", RemotePath));
    }

    [Fact]
    public async Task when_upload_async_is_called_with_pre_cancelled_token_then_operation_is_cancelled()
    {
        string localPath = Path.GetTempFileName();
        try
        {
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            var sut = new UploadService(Substitute.For<IHttpClientFactory>());

            await Should.ThrowAsync<OperationCanceledException>(() =>
                sut.UploadAsync(BuildAnonymousGraphClient(), DriveId, ParentFolderId, localPath, RemotePath, ct: cts.Token));
        }
        finally
        {
            File.Delete(localPath);
        }
    }

    [Fact]
    public async Task when_chunk_returns_201_created_with_item_id_then_item_id_is_returned()
    {
        string localPath = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(localPath, new byte[64], TestContext.Current.CancellationToken);
            using var server = WireMockServer.Start();

            server.Given(WireMockRequest.Create().UsingPost())
                  .RespondWith(Response.Create().WithStatusCode(200).WithBody(SessionJson(server)).WithHeader("Content-Type", "application/json"));

            server.Given(WireMockRequest.Create().UsingPut().WithPath("/chunk-upload"))
                  .RespondWith(Response.Create().WithCallback(_ => Created201Response()));

            var factory = CreateChunkClientFactory(server);
            var sut = new UploadService(factory);

            string itemId = await sut.UploadAsync(BuildGraphClient(server), DriveId, ParentFolderId, localPath, RemotePath, ct: TestContext.Current.CancellationToken);

            itemId.ShouldBe(ExpectedItemId);
        }
        finally
        {
            File.Delete(localPath);
        }
    }

    [Fact]
    public async Task when_chunk_returns_200_ok_with_item_id_then_item_id_is_returned()
    {
        string localPath = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(localPath, new byte[64], TestContext.Current.CancellationToken);
            using var server = WireMockServer.Start();

            server.Given(WireMockRequest.Create().UsingPost())
                  .RespondWith(Response.Create().WithStatusCode(200).WithBody(SessionJson(server)).WithHeader("Content-Type", "application/json"));

            server.Given(WireMockRequest.Create().UsingPut().WithPath("/chunk-upload"))
                  .RespondWith(Response.Create().WithCallback(_ => Ok200Response()));

            var factory = CreateChunkClientFactory(server);
            var sut = new UploadService(factory);

            string itemId = await sut.UploadAsync(BuildGraphClient(server), DriveId, ParentFolderId, localPath, RemotePath, ct: TestContext.Current.CancellationToken);

            itemId.ShouldBe(ExpectedItemId);
        }
        finally
        {
            File.Delete(localPath);
        }
    }

    [Fact]
    public async Task when_chunk_returns_202_accepted_then_upload_continues_to_next_chunk()
    {
        string localPath = Path.GetTempFileName();
        try
        {
            int chunkSizeBytes = 10 * 1024 * 1024;
            byte[] fileBytes = new byte[chunkSizeBytes + 64];
            await File.WriteAllBytesAsync(localPath, fileBytes, TestContext.Current.CancellationToken);
            using var server = WireMockServer.Start();

            server.Given(WireMockRequest.Create().UsingPost())
                  .RespondWith(Response.Create().WithStatusCode(200).WithBody(SessionJson(server)).WithHeader("Content-Type", "application/json"));

            int putCallCount = 0;
            server.Given(WireMockRequest.Create().UsingPut().WithPath("/chunk-upload"))
                  .RespondWith(Response.Create()
                      .WithCallback(_ =>
                      {
                          int callIndex = System.Threading.Interlocked.Increment(ref putCallCount) - 1;
                          return callIndex == 0 ? Accepted202Response() : Created201Response();
                      }));

            var factory = CreateChunkClientFactory(server);
            var sut = new UploadService(factory);

            string itemId = await sut.UploadAsync(BuildGraphClient(server), DriveId, ParentFolderId, localPath, RemotePath, ct: TestContext.Current.CancellationToken);

            itemId.ShouldBe(ExpectedItemId);
            putCallCount.ShouldBe(2);
        }
        finally
        {
            File.Delete(localPath);
        }
    }

    [Fact]
    public async Task when_upload_reports_progress_then_progress_is_reported_with_byte_count()
    {
        string localPath = Path.GetTempFileName();
        try
        {
            const int FileSize = 64;
            await File.WriteAllBytesAsync(localPath, new byte[FileSize], TestContext.Current.CancellationToken);
            using var server = WireMockServer.Start();

            server.Given(WireMockRequest.Create().UsingPost())
                  .RespondWith(Response.Create().WithStatusCode(200).WithBody(SessionJson(server)).WithHeader("Content-Type", "application/json"));

            server.Given(WireMockRequest.Create().UsingPut().WithPath("/chunk-upload"))
                  .RespondWith(Response.Create().WithCallback(_ => Created201Response()));

            var reportedValues = new List<long>();
            var progress = new Progress<long>(reportedValues.Add);
            var factory = CreateChunkClientFactory(server);
            var sut = new UploadService(factory);

            await sut.UploadAsync(BuildGraphClient(server), DriveId, ParentFolderId, localPath, RemotePath, progress, TestContext.Current.CancellationToken);

            await Task.Delay(50, TestContext.Current.CancellationToken);

            reportedValues.ShouldNotBeEmpty();
            reportedValues[^1].ShouldBe(FileSize);
        }
        finally
        {
            File.Delete(localPath);
        }
    }

    private static ResponseMessage Accepted202Response() =>
        new() { StatusCode = 202, Headers = JsonContentTypeHeaders() };

    private static ResponseMessage Created201Response()
    {
        var msg = new ResponseMessage { StatusCode = 201, Headers = JsonContentTypeHeaders() };
        msg.BodyData = new BodyData { DetectedBodyType = WireMockBodyType.String, BodyAsString = ItemIdJson() };

        return msg;
    }

    private static ResponseMessage Ok200Response()
    {
        var msg = new ResponseMessage { StatusCode = 200, Headers = JsonContentTypeHeaders() };
        msg.BodyData = new BodyData { DetectedBodyType = WireMockBodyType.String, BodyAsString = ItemIdJson() };

        return msg;
    }

    private static Dictionary<string, WireMockList<string>> JsonContentTypeHeaders() =>
        new() { { "Content-Type", new WireMockList<string>("application/json") } };
}
