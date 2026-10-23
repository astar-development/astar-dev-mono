using AStarDev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Kiota.Http.HttpClientLibrary;
using WireMock.Types;
using WireMock.Util;
using WireMockRequest = WireMock.RequestBuilders.Request;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Sync.Jobs;

public sealed class GivenAnUploadServiceRetryingPreconditionFailures
{
    private const string LocalFilePath = "/mock/test-file.bin";
    private const string ErrorJson = "{\"error\":{\"code\":\"resourceModified\",\"message\":\"The resource has changed since the caller last read it; usually an eTag mismatch\"}}";

    private readonly FakeTimeProvider timeProvider = new();

    private static GraphServiceClient BuildGraphClient(WireMockServer server)
        => new(new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: new HttpClient { BaseAddress = new Uri(server.Urls[0]) }));

    private static IHttpClientFactory CreateChunkClientFactory()
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(new HttpClient());

        return factory;
    }

    private static MockFileSystem CreateFileSystem()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile(LocalFilePath).Which(m => m.HasBytesContent(new byte[64]));

        return mockFileSystem;
    }

    private UploadService CreateSut() => new(CreateChunkClientFactory(), CreateFileSystem(), Substitute.For<ILogger<UploadService>>(), timeProvider);

    private async Task<Result<string, string>> UploadAsync(WireMockServer server)
    {
        var upload = CreateSut().UploadAsync(BuildGraphClient(server), new DriveId("drive-001"), "folder-001", LocalFilePath, "folder/test-file.bin", cancellationToken: TestContext.Current.CancellationToken);

        while (!upload.IsCompleted)
        {
            timeProvider.Advance(TimeSpan.FromSeconds(30));
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        return await upload;
    }

    private static ResponseMessage JsonResponse(int statusCode, string body)
        => new()
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, WireMockList<string>> { ["Content-Type"] = new("application/json") },
            BodyData = new BodyData { DetectedBodyType = BodyType.String, BodyAsString = body }
        };

    private static void StubChunkUpload(WireMockServer server)
        => server.Given(WireMockRequest.Create().UsingPut().WithPath("/chunk-upload"))
            .RespondWith(Response.Create().WithStatusCode(201).WithHeader("Content-Type", "application/json").WithBody("{\"id\":\"item-1\"}"));

    [Fact]
    public async Task when_session_creation_returns_412_once_then_the_upload_is_retried_and_succeeds()
    {
        using var server = WireMockServer.Start();
        int sessionRequests = 0;
        server.Given(WireMockRequest.Create().UsingPost())
            .RespondWith(Response.Create().WithCallback(_ => Interlocked.Increment(ref sessionRequests) == 1
                ? JsonResponse(412, ErrorJson)
                : JsonResponse(200, $"{{\"uploadUrl\":\"{server.Urls[0]}/chunk-upload\"}}")));
        StubChunkUpload(server);

        var result = await UploadAsync(server);

        result.ShouldBeOfType<Ok<string, string>>().Value.ShouldBe("item-1");
    }

    [Fact]
    public async Task when_session_creation_returns_412_every_time_then_the_upload_stops_after_the_maximum_retries()
    {
        using var server = WireMockServer.Start();
        server.Given(WireMockRequest.Create().UsingPost())
            .RespondWith(Response.Create().WithStatusCode(412).WithHeader("Content-Type", "application/json").WithBody(ErrorJson));

        await Should.ThrowAsync<ODataError>(() => UploadAsync(server));

        server.LogEntries.Count.ShouldBe(6);
    }

    [Fact]
    public async Task when_session_creation_returns_another_error_then_it_is_not_retried()
    {
        using var server = WireMockServer.Start();
        server.Given(WireMockRequest.Create().UsingPost())
            .RespondWith(Response.Create().WithStatusCode(403).WithHeader("Content-Type", "application/json").WithBody("{\"error\":{\"code\":\"accessDenied\",\"message\":\"no\"}}"));

        await Should.ThrowAsync<ODataError>(() => UploadAsync(server));

        server.LogEntries.Count.ShouldBe(1);
    }
}
