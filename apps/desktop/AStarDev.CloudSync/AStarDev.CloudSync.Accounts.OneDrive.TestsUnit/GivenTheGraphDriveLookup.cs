using System.Net;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Accounts.OneDrive.TestsUnit;

public sealed class GivenTheGraphDriveLookup : IDisposable
{
    private readonly RecordingLoggerProvider logs = new();
    private readonly FakeAccountStore accountStore = new();

    [Fact]
    public async Task when_graph_returns_the_drive_then_its_id_and_quota_are_saved()
    {
        _ = await AddAsync(new FakeGraphHandler(), TestContext.Current.CancellationToken);

        accountStore.SavedRegistrations.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            registration => registration.DriveId.ShouldBe(TestSignIns.Drive.DriveId),
            registration => registration.Storage.ShouldBe(TestSignIns.Drive.Storage));
    }

    [Fact]
    public async Task when_the_drive_is_read_then_me_drive_is_requested_with_the_access_token_through_the_named_client()
    {
        var fakes = Fakes(new FakeGraphHandler());

        _ = await new OneDriveModule(fakes).AddAsync(TestContext.Current.CancellationToken);

        fakes.Graph.Requests.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            request => request.Method.ShouldBe(HttpMethod.Get),
            request => request.RequestUri!.AbsoluteUri.ShouldStartWith("https://graph.microsoft.com/v1.0/me/drive"),
            request => request.Headers.Authorization!.ToString().ShouldBe($"Bearer {TestSignIns.AccessToken}"));
        fakes.HttpClientFactory.RequestedNames.ShouldBe(["MicrosoftGraph"]);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task when_graph_refuses_the_token_then_the_failure_is_auth_rejected(HttpStatusCode statusCode)
    {
        var handler = new FakeGraphHandler { Respond = (_, _) => Task.FromResult(FakeGraphHandler.Json(statusCode, """{ "error": { "code": "InvalidAuthenticationToken", "message": "Access token is empty." } }""")) };

        var outcome = await AddAsync(handler, TestContext.Current.CancellationToken);

        outcome.ShouldBe(AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateAuthRejected()));
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task when_graph_returns_an_error_then_the_drive_is_unavailable_and_a_warning_is_logged(HttpStatusCode statusCode)
    {
        var handler = new FakeGraphHandler { Respond = (_, _) => Task.FromResult(FakeGraphHandler.Json(statusCode, """{ "error": { "code": "itemNotFound", "message": "captain.flint@example.com has no drive" } }""")) };

        var outcome = await AddAsync(handler, TestContext.Current.CancellationToken);

        outcome.ShouldBe(AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateDriveUnavailable()));
        logs.Logger.EventNames.ShouldContain("DriveLookupFailed");
        logs.Logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task when_the_network_fails_then_the_failure_is_network()
    {
        var handler = new FakeGraphHandler { Respond = (_, _) => throw new HttpRequestException("Name or service not known") };

        var outcome = await AddAsync(handler, TestContext.Current.CancellationToken);

        outcome.ShouldBe(AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateNetwork()));
    }

    [Fact]
    public async Task when_graph_returns_a_drive_without_an_id_then_the_drive_is_unavailable()
    {
        var handler = new FakeGraphHandler { Respond = (_, _) => Task.FromResult(FakeGraphHandler.Json(HttpStatusCode.OK, """{ "quota": { "used": 1, "total": 2 } }""")) };

        var outcome = await AddAsync(handler, TestContext.Current.CancellationToken);

        outcome.ShouldBe(AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateDriveUnavailable()));
    }

    [Fact]
    public async Task when_graph_returns_no_quota_then_the_storage_is_zero()
    {
        var handler = new FakeGraphHandler { Respond = (_, _) => Task.FromResult(FakeGraphHandler.Json(HttpStatusCode.OK, """{ "id": "8f2a1b3c4d5e6f70" }""")) };

        _ = await AddAsync(handler, TestContext.Current.CancellationToken);

        accountStore.SavedRegistrations.ShouldHaveSingleItem().Storage.ShouldBe(StorageUsageFactory.Create(0, 0));
    }

    [Fact]
    public async Task when_the_lookup_fails_then_the_log_contains_neither_the_token_nor_the_graph_error_message()
    {
        var handler = new FakeGraphHandler { Respond = (_, _) => Task.FromResult(FakeGraphHandler.Json(HttpStatusCode.Forbidden, """{ "error": { "code": "accessDenied", "message": "captain.flint@example.com is not allowed" } }""")) };

        _ = await AddAsync(handler, TestContext.Current.CancellationToken);

        logs.Logger.Messages.ShouldAllBe(message => !message.Contains(TestSignIns.AccessToken) && !message.Contains(TestSignIns.Email));
        logs.Logger.Entries.ShouldAllBe(entry => entry.Exception == null);
    }

    [Fact]
    public async Task when_the_lookup_is_cancelled_then_the_attempt_is_cancelled_and_nothing_is_saved()
    {
        using var cancellation = new CancellationTokenSource();
        var handler = new FakeGraphHandler
        {
            Respond = async (_, token) =>
            {
                await cancellation.CancelAsync();
                token.ThrowIfCancellationRequested();

                return FakeGraphHandler.Json(HttpStatusCode.OK, FakeGraphHandler.DriveJson);
            },
        };

        var outcome = await AddAsync(handler, cancellation.Token);

        outcome.ShouldBe(AccountAddOutcomeFactory.CreateCancelled());
        accountStore.SavedRegistrations.ShouldBeEmpty();
    }

    private Task<AccountAddOutcome> AddAsync(FakeGraphHandler handler, CancellationToken cancellationToken) => new OneDriveModule(Fakes(handler)).AddAsync(cancellationToken);

    private OneDriveFakes Fakes(FakeGraphHandler handler) => new() { Graph = handler, AccountStore = accountStore, Logs = logs };

    public void Dispose()
    {
        logs.Dispose();
    }
}
