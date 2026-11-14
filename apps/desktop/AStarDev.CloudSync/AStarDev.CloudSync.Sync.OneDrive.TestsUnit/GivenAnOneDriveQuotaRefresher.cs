using System.Net;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Accounts.OneDrive;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;
using NSubstitute;

namespace AStarDev.CloudSync.Sync.OneDrive.TestsUnit;

public sealed class GivenAnOneDriveQuotaRefresher : IDisposable
{
    private const string Drive = "8A3F2C1D0E9B7A65";
    private const string Token = "secret-token-123";
    private const string Email = "personal@example.com";

    private readonly GraphScript graph = new();
    private readonly InMemoryAccountStore accountStore = new();
    private readonly FakeAccessTokenSource tokenSource = new();
    private readonly IAccountThrottleGate gate = Substitute.For<IAccountThrottleGate>();
    private readonly RecordingLoggerProvider logs = new();
    private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 11, 14, 9, 0, 0, TimeSpan.Zero));
    private ServiceProvider? provider;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_the_drive_reports_a_quota_then_the_used_and_total_bytes_come_back()
    {
        graph.Respond = _ => GraphScript.Json(HttpStatusCode.OK, "{\"id\":\"" + Drive + "\",\"quota\":{\"used\":1234,\"total\":56789}}");
        var account = await AddAccountAsync();

        var result = await Refresher().RefreshAsync(account, CancellationToken);

        result.Match(usage => (object)usage, error => error).ShouldBe(StorageUsageFactory.Create(1_234, 56_789));
    }

    [Fact]
    public async Task when_the_quota_is_asked_for_then_the_request_reads_the_account_drive_with_its_token_through_the_throttle_gate()
    {
        graph.Respond = _ => GraphScript.Json(HttpStatusCode.OK, "{\"id\":\"" + Drive + "\",\"quota\":{\"used\":1,\"total\":2}}");
        var account = await AddAccountAsync();

        _ = await Refresher().RefreshAsync(account, CancellationToken);

        var request = graph.Requests.ShouldHaveSingleItem();
        request.Url.AbsolutePath.ShouldBe($"/v1.0/drives/{Drive}");
        request.Authorization.ShouldBe($"Bearer {Token}");
        await gate.Received(1).WaitAsync(account, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("{\"id\":\"d\"}")]
    [InlineData("{\"id\":\"d\",\"quota\":null}")]
    [InlineData("{}")]
    public async Task when_the_drive_gives_no_quota_then_the_result_is_unexpected_rather_than_a_usage_of_zero(string body)
    {
        graph.Respond = _ => GraphScript.Json(HttpStatusCode.OK, body);
        var account = await AddAccountAsync();

        var result = await Refresher().RefreshAsync(account, CancellationToken);

        result.Match(_ => (object)"usage", error => error).ShouldBe(ProviderErrorFactory.CreateUnexpected());
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "InvalidAuthenticationToken", "AuthenticationRequired")]
    [InlineData(HttpStatusCode.Forbidden, "accessDenied", "AccessDenied")]
    [InlineData(HttpStatusCode.NotFound, "itemNotFound", "NotFound")]
    [InlineData(HttpStatusCode.InternalServerError, "generalException", "Transient")]
    public async Task when_graph_returns_an_error_then_it_maps_to_a_provider_error_and_the_warning_has_only_the_status_and_code(HttpStatusCode status, string graphCode, string expected)
    {
        graph.Respond = _ => GraphScript.Json(status, "{\"error\":{\"code\":\"" + graphCode + "\",\"message\":\"for " + Email + " token " + Token + "\"}}");
        var account = await AddAccountAsync();

        var result = await Refresher().RefreshAsync(account, CancellationToken);

        result.Match(_ => "usage", error => error.GetType().Name).ShouldBe(expected);
        logs.Logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning);
        logs.Logger.Messages.ShouldContain(message => message.Contains(graphCode, StringComparison.Ordinal));
        logs.Logger.Messages.ShouldAllBe(message => !message.Contains(Email, StringComparison.Ordinal) && !message.Contains(Token, StringComparison.Ordinal));
    }

    [Fact]
    public async Task when_graph_returns_a_very_long_error_code_then_the_logged_code_is_capped()
    {
        var longCode = new string('x', 500) + "jane.doe@example.com";
        graph.Respond = _ => GraphScript.Json(HttpStatusCode.InternalServerError, "{\"error\":{\"code\":\"" + longCode + "\",\"message\":\"m\"}}");
        var account = await AddAccountAsync();

        _ = await Refresher().RefreshAsync(account, CancellationToken);

        logs.Logger.Messages.ShouldContain(message => message.Contains(new string('x', 64), StringComparison.Ordinal));
        logs.Logger.Messages.ShouldAllBe(message => !message.Contains(new string('x', 65), StringComparison.Ordinal) && !message.Contains(Email, StringComparison.Ordinal));
    }

    [Fact]
    public async Task when_the_request_times_out_without_the_caller_cancelling_then_the_error_is_transient()
    {
        graph.Respond = _ => throw new TaskCanceledException("The request timed out.");
        var account = await AddAccountAsync();

        var result = await Refresher().RefreshAsync(account, CancellationToken);

        result.Match(_ => (object)"usage", error => error).ShouldBe(ProviderErrorFactory.CreateTransient());
    }

    [Fact]
    public async Task when_the_caller_cancels_then_the_refresh_is_cancelled()
    {
        using var cancelled = new CancellationTokenSource();
        var account = await AddAccountAsync();
        graph.Respond = _ =>
        {
            cancelled.Cancel();

            throw new OperationCanceledException(cancelled.Token);
        };

        var refresh = Refresher().RefreshAsync(account, cancelled.Token);

        await Should.ThrowAsync<OperationCanceledException>(refresh);
    }

    [Theory]
    [InlineData("SignInRequired", "AuthenticationRequired")]
    [InlineData("Unavailable", "Transient")]
    [InlineData("UnknownAccount", "NotFound")]
    public async Task when_no_token_can_be_had_then_the_matching_error_is_returned_and_graph_is_not_called(string outcome, string expected)
    {
        tokenSource.Outcome = outcome switch
        {
            "SignInRequired" => AccessTokenOutcomeFactory.CreateSignInRequired(),
            "UnknownAccount" => AccessTokenOutcomeFactory.CreateUnknownAccount(),
            _ => AccessTokenOutcomeFactory.CreateUnavailable(),
        };
        var account = await AddAccountAsync();

        var result = await Refresher().RefreshAsync(account, CancellationToken);

        result.Match(_ => "usage", error => error.GetType().Name).ShouldBe(expected);
        graph.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_account_is_not_stored_then_it_is_not_found_and_no_token_is_asked_for()
    {
        var result = await Refresher().RefreshAsync(AccountIdFactory.Create(Guid.NewGuid()), CancellationToken);

        result.Match(_ => (object)"usage", error => error).ShouldBe(ProviderErrorFactory.CreateNotFound());
        tokenSource.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task when_many_refreshes_run_at_once_then_each_reads_the_quota()
    {
        graph.Respond = _ => GraphScript.Json(HttpStatusCode.OK, "{\"id\":\"d\",\"quota\":{\"used\":10,\"total\":20}}");
        var account = await AddAccountAsync();
        var refresher = Refresher();

        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => refresher.RefreshAsync(account, CancellationToken), CancellationToken)));

        results.ShouldAllBe(result => result.Match(usage => usage == StorageUsageFactory.Create(10, 20), _ => false));
        graph.Requests.Count.ShouldBe(20);
    }

    [Fact]
    public void when_the_module_is_added_then_the_quota_refresher_is_registered_keyed_by_onedrive()
    {
        var services = new ServiceCollection().AddOneDriveSync();

        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(IQuotaRefresher) && descriptor.IsKeyedService && Equals(descriptor.ServiceKey, CloudProviderFactory.CreateOneDrive()) && descriptor.Lifetime == ServiceLifetime.Singleton);
    }

    public void Dispose()
    {
        provider?.Dispose();
        graph.Dispose();
        logs.Dispose();
    }

    private async Task<AccountId> AddAccountAsync()
    {
        var key = ProviderAccountKeyFactory.Create("key-1").Match(created => created, _ => throw new InvalidOperationException("The test key must be valid."));
        var registration = AccountRegistrationFactory.Create(key, CloudProviderFactory.CreateOneDrive(), "Personal", Email, new DriveId(Drive), StorageUsageFactory.Create(1, 2));

        return (await accountStore.AddAsync(registration, CancellationToken)).ShouldBeOfType<AccountAddOutcome.Added>().AccountId;
    }

    private IQuotaRefresher Refresher()
    {
        var services = new ServiceCollection()
            .AddLogging(logging => logging.ClearProviders().AddProvider(logs))
            .AddSingleton<TimeProvider>(time)
            .AddSingleton<IAccountStore>(accountStore)
            .AddSingleton<IAccessTokenSource>(tokenSource)
            .AddSingleton(gate)
            .AddSingleton(new RetryHandlerOption { Delay = 0, MaxRetry = 1 })
            .AddOneDriveSync();
        _ = services.AddHttpClient(OneDriveSyncHttp.ClientName).ConfigurePrimaryHttpMessageHandler(() => graph);
        provider?.Dispose();
        provider = services.BuildServiceProvider();

        return provider.GetRequiredKeyedService<IQuotaRefresher>(CloudProviderFactory.CreateOneDrive());
    }
}
