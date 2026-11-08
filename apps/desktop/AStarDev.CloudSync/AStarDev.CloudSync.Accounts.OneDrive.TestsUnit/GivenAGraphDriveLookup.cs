using System.Net;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Accounts.OneDrive.TestsUnit;

public class GivenAGraphDriveLookup
{
    private const string DriveJson = """{ "id": "8f2a1b3c4d5e6f70", "driveType": "personal", "quota": { "used": 1500, "total": 5000 } }""";

    private readonly RecordingLogger<GraphDriveLookup> logger = new();

    [Fact]
    public async Task when_graph_returns_the_drive_then_its_id_and_quota_are_returned()
    {
        var handler = new FakeGraphHandler { Respond = (_, _) => Task.FromResult(FakeGraphHandler.Json(HttpStatusCode.OK, DriveJson)) };

        var drive = await CreateLookup(handler).GetDriveAsync(TestSignIns.AccessToken, TestContext.Current.CancellationToken).MatchAsync(found => (object)found, error => error);

        drive.ShouldBe(TestSignIns.Drive);
    }

    [Fact]
    public async Task when_the_drive_is_read_then_me_drive_is_requested_with_the_access_token_through_the_named_client()
    {
        var handler = new FakeGraphHandler { Respond = (_, _) => Task.FromResult(FakeGraphHandler.Json(HttpStatusCode.OK, DriveJson)) };
        var httpClientFactory = new FakeHttpClientFactory(handler);

        _ = await new GraphDriveLookup(httpClientFactory, logger).GetDriveAsync(TestSignIns.AccessToken, TestContext.Current.CancellationToken);

        handler.Requests.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            request => request.Method.ShouldBe(HttpMethod.Get),
            request => request.RequestUri!.AbsoluteUri.ShouldStartWith("https://graph.microsoft.com/v1.0/me/drive"),
            request => request.Headers.Authorization!.ToString().ShouldBe($"Bearer {TestSignIns.AccessToken}"));
        httpClientFactory.RequestedNames.ShouldBe([GraphDriveLookup.HttpClientName]);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task when_graph_refuses_the_token_then_the_failure_is_auth_rejected(HttpStatusCode statusCode)
    {
        var handler = new FakeGraphHandler { Respond = (_, _) => Task.FromResult(FakeGraphHandler.Json(statusCode, """{ "error": { "code": "InvalidAuthenticationToken", "message": "Access token is empty." } }""")) };

        var failure = await CreateLookup(handler).GetDriveAsync(TestSignIns.AccessToken, TestContext.Current.CancellationToken).MatchAsync(drive => (object)drive, error => error);

        failure.ShouldBe(AccountAddFailureFactory.CreateAuthRejected());
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task when_graph_returns_an_error_then_the_drive_is_unavailable_and_a_warning_is_logged(HttpStatusCode statusCode)
    {
        var handler = new FakeGraphHandler { Respond = (_, _) => Task.FromResult(FakeGraphHandler.Json(statusCode, """{ "error": { "code": "itemNotFound", "message": "captain.flint@example.com has no drive" } }""")) };

        var failure = await CreateLookup(handler).GetDriveAsync(TestSignIns.AccessToken, TestContext.Current.CancellationToken).MatchAsync(drive => (object)drive, error => error);

        failure.ShouldBe(AccountAddFailureFactory.CreateDriveUnavailable());
        logger.Entries.ShouldContain(entry => entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning);
    }

    [Fact]
    public async Task when_the_network_fails_then_the_failure_is_network()
    {
        var handler = new FakeGraphHandler { Respond = (_, _) => throw new HttpRequestException("Name or service not known") };

        var failure = await CreateLookup(handler).GetDriveAsync(TestSignIns.AccessToken, TestContext.Current.CancellationToken).MatchAsync(drive => (object)drive, error => error);

        failure.ShouldBe(AccountAddFailureFactory.CreateNetwork());
    }

    [Fact]
    public async Task when_graph_returns_a_drive_without_an_id_then_the_drive_is_unavailable()
    {
        var handler = new FakeGraphHandler { Respond = (_, _) => Task.FromResult(FakeGraphHandler.Json(HttpStatusCode.OK, """{ "quota": { "used": 1, "total": 2 } }""")) };

        var failure = await CreateLookup(handler).GetDriveAsync(TestSignIns.AccessToken, TestContext.Current.CancellationToken).MatchAsync(drive => (object)drive, error => error);

        failure.ShouldBe(AccountAddFailureFactory.CreateDriveUnavailable());
    }

    [Fact]
    public async Task when_graph_returns_no_quota_then_the_storage_is_zero()
    {
        var handler = new FakeGraphHandler { Respond = (_, _) => Task.FromResult(FakeGraphHandler.Json(HttpStatusCode.OK, """{ "id": "8f2a1b3c4d5e6f70" }""")) };

        var storage = await CreateLookup(handler).GetDriveAsync(TestSignIns.AccessToken, TestContext.Current.CancellationToken).MatchAsync(drive => (object)drive.Storage, error => error);

        storage.ShouldBe(StorageUsageFactory.Create(0, 0));
    }

    [Fact]
    public async Task when_the_lookup_fails_then_the_log_contains_neither_the_token_nor_the_graph_error_message()
    {
        var handler = new FakeGraphHandler { Respond = (_, _) => Task.FromResult(FakeGraphHandler.Json(HttpStatusCode.Forbidden, """{ "error": { "code": "accessDenied", "message": "captain.flint@example.com is not allowed" } }""")) };

        _ = await CreateLookup(handler).GetDriveAsync(TestSignIns.AccessToken, TestContext.Current.CancellationToken);

        logger.Messages.ShouldAllBe(message => !message.Contains(TestSignIns.AccessToken) && !message.Contains(TestSignIns.Email));
        logger.Entries.ShouldAllBe(entry => entry.Exception == null);
    }

    [Fact]
    public async Task when_the_lookup_is_cancelled_then_the_cancellation_is_not_swallowed()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => CreateLookup(new FakeGraphHandler()).GetDriveAsync(TestSignIns.AccessToken, cancellation.Token));
    }

    private GraphDriveLookup CreateLookup(FakeGraphHandler handler) => new(new FakeHttpClientFactory(handler), logger);
}
