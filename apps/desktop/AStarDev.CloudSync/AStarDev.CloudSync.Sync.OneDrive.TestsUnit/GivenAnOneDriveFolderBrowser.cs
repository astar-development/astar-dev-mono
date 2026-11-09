using System.Net;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;
using NSubstitute;
using AStarDev.CloudSync.Accounts.OneDrive;

namespace AStarDev.CloudSync.Sync.OneDrive.TestsUnit;

public sealed class GivenAnOneDriveFolderBrowser : IDisposable
{
    private const string Drive = "8A3F2C1D0E9B7A65";
    private const string Token = "secret-token-123";
    private const string Children = "https://graph.microsoft.com/v1.0/drives/" + Drive + "/items/";

    private readonly GraphScript graph = new();
    private readonly InMemoryAccountStore accountStore = new();
    private readonly FakeAccessTokenSource tokenSource = new();
    private readonly IAccountThrottleGate gate = Substitute.For<IAccountThrottleGate>();
    private readonly RecordingLoggerProvider logs = new();
    private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.Zero));
    private ServiceProvider? provider;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_the_root_is_listed_then_only_real_folders_come_back_sorted_with_their_parent_and_child_flags()
    {
        graph.Respond = _ => GraphScript.Json(HttpStatusCode.OK, Page(
            Folder("b", "beta", 0), Folder("a", "Alpha", 3), Folder("z", "zulu", 1),
            "{\"id\":\"file\",\"name\":\"notes.txt\",\"file\":{}}",
            "{\"id\":\"book\",\"name\":\"Notebook\",\"folder\":{\"childCount\":1},\"package\":{\"type\":\"oneNote\"}}",
            "{\"id\":\"shared\",\"name\":\"Shared\",\"folder\":{\"childCount\":1},\"remoteItem\":{\"id\":\"other\"}}"));
        var account = await AddAccountAsync();

        var result = await Browser().GetChildFoldersAsync(account, Option.None<RemoteItemId>(), CancellationToken);

        result.Match(folders => folders.Select(folder => (folder.Name, folder.Id.Value, folder.ParentId.Value, folder.Children)), _ => []).ShouldBe([
            ("Alpha", "a", "root-id", RemoteFolderChildrenFactory.CreateMayHaveChildFolders()),
            ("beta", "b", "root-id", RemoteFolderChildrenFactory.CreateNoChildFolders()),
            ("zulu", "z", "root-id", RemoteFolderChildrenFactory.CreateMayHaveChildFolders())]);
    }

    [Fact]
    public async Task when_the_root_is_listed_then_the_request_asks_for_the_drive_root_children_with_the_selected_fields_and_a_page_size()
    {
        var account = await AddAccountAsync();

        _ = await Browser().GetChildFoldersAsync(account, Option.None<RemoteItemId>(), CancellationToken);

        var request = graph.Requests.ShouldHaveSingleItem();
        request.Url.AbsolutePath.ShouldBe($"/v1.0/drives/{Drive}/items/root/children");
        request.Url.Query.ShouldContain("$top=200");
        request.Url.Query.ShouldContain("$select=id,name,folder,package,remoteItem,parentReference");
        request.Authorization.ShouldBe($"Bearer {Token}");
    }

    [Fact]
    public async Task when_a_sub_folder_is_listed_then_the_request_names_that_folder()
    {
        var account = await AddAccountAsync();

        _ = await Browser().GetChildFoldersAsync(account, Option.Some(ItemId("folder-42")), CancellationToken);

        graph.Requests.ShouldHaveSingleItem().Url.AbsolutePath.ShouldBe($"/v1.0/drives/{Drive}/items/folder-42/children");
    }

    [Fact]
    public async Task when_a_request_is_made_then_it_passes_the_account_throttle_gate_for_that_account()
    {
        var account = await AddAccountAsync();

        _ = await Browser().GetChildFoldersAsync(account, Option.None<RemoteItemId>(), CancellationToken);

        await gate.Received(1).WaitAsync(account, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_folder_has_three_pages_of_child_folders_then_all_of_them_are_returned()
    {
        graph.Respond = request => request.RequestUri!.Query.Contains("page=3", StringComparison.Ordinal)
            ? GraphScript.Json(HttpStatusCode.OK, Page(Folder("f5", "five", 0)))
            : request.RequestUri.Query.Contains("page=2", StringComparison.Ordinal)
                ? GraphScript.Json(HttpStatusCode.OK, Page(Folder("f3", "three", 0), Folder("f4", "four", 0)) .Replace("\"value\"", "\"@odata.nextLink\":\"" + Children + "root/children?page=3\",\"value\""))
                : GraphScript.Json(HttpStatusCode.OK, Page(Folder("f1", "one", 0), Folder("f2", "two", 0)).Replace("\"value\"", "\"@odata.nextLink\":\"" + Children + "root/children?page=2\",\"value\""));
        var account = await AddAccountAsync();

        var result = await Browser().GetChildFoldersAsync(account, Option.None<RemoteItemId>(), CancellationToken);

        result.Match(folders => folders.Select(folder => folder.Name).ToList(), _ => []).ShouldBe(["five", "four", "one", "three", "two"]);
        graph.Requests.Count.ShouldBe(3);
    }

    [Theory]
    [InlineData("https://evil.example.com/v1.0/drives/x/items/root/children?page=2")]
    [InlineData("http://graph.microsoft.com/v1.0/drives/x/items/root/children?page=2")]
    [InlineData("https://graph.microsoft.com.evil.example.com/v1.0/x")]
    [InlineData("not a url")]
    public async Task when_a_next_link_is_not_https_on_the_graph_host_then_paging_stops_unexpected_and_the_link_is_never_followed(string nextLink)
    {
        graph.Respond = _ => GraphScript.Json(HttpStatusCode.OK, Page(Folder("f1", "one", 0)).Replace("\"value\"", "\"@odata.nextLink\":\"" + nextLink + "\",\"value\""));
        var account = await AddAccountAsync();

        var result = await Browser().GetChildFoldersAsync(account, Option.None<RemoteItemId>(), CancellationToken);

        result.Match(_ => (object)"listed", error => error).ShouldBe(ProviderErrorFactory.CreateUnexpected());
        graph.Requests.Count.ShouldBe(1);
        logs.Logger.Entries.ShouldContain(entry => entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning);
    }

    [Fact]
    public async Task when_a_next_link_repeats_then_paging_stops_instead_of_looping_forever()
    {
        graph.Respond = _ => GraphScript.Json(HttpStatusCode.OK, Page(Folder("f1", "one", 0)).Replace("\"value\"", "\"@odata.nextLink\":\"" + Children + "root/children?page=2\",\"value\""));
        var account = await AddAccountAsync();

        var result = await Browser().GetChildFoldersAsync(account, Option.None<RemoteItemId>(), CancellationToken);

        result.Match(_ => (object)"listed", error => error).ShouldBe(ProviderErrorFactory.CreateUnexpected());
        graph.Requests.Count.ShouldBe(2);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "InvalidAuthenticationToken", "AuthenticationRequired")]
    [InlineData(HttpStatusCode.Forbidden, "accessDenied", "AccessDenied")]
    [InlineData(HttpStatusCode.NotFound, "itemNotFound", "NotFound")]
    [InlineData(HttpStatusCode.InternalServerError, "generalException", "Transient")]
    [InlineData(HttpStatusCode.BadGateway, "badGateway", "Transient")]
    [InlineData(HttpStatusCode.GatewayTimeout, "none", "Transient")]
    [InlineData(HttpStatusCode.BadRequest, "invalidRequest", "Unexpected")]
    [InlineData(HttpStatusCode.Gone, "resyncRequired", "Unexpected")]
    public async Task when_graph_answers_with_an_error_status_then_it_maps_to_the_provider_error(HttpStatusCode status, string graphCode, string expected)
    {
        graph.Respond = _ => GraphScript.Json(status, "{\"error\":{\"code\":\"" + graphCode + "\",\"message\":\"Secret folder name Taxes\"}}");
        var account = await AddAccountAsync();

        var result = await Browser().GetChildFoldersAsync(account, Option.None<RemoteItemId>(), CancellationToken);

        result.Match(_ => "listed", error => error.GetType().Name).ShouldBe(expected);
        logs.Logger.Messages.ShouldContain(message => message.Contains(graphCode, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task when_graph_keeps_throttling_then_throttled_is_returned_with_the_retry_after_after_the_sdk_used_its_retry(HttpStatusCode status)
    {
        var response = () =>
        {
            var throttled = GraphScript.Json(status, "{\"error\":{\"code\":\"activityLimitReached\",\"message\":\"Slow down\"}}");
            throttled.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(1));

            return throttled;
        };
        graph.Respond = _ => response();
        var account = await AddAccountAsync();

        var result = await Browser(new RetryHandlerOption { Delay = 0, MaxRetry = 1 }).GetChildFoldersAsync(account, Option.None<RemoteItemId>(), CancellationToken);

        result.Match(_ => (object)"listed", error => error).ShouldBe(ProviderErrorFactory.CreateThrottled(Option.Some(TimeSpan.FromSeconds(1))));
        graph.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task when_the_network_fails_then_the_error_is_transient()
    {
        graph.Respond = _ => throw new HttpRequestException("offline");
        var account = await AddAccountAsync();

        var result = await Browser().GetChildFoldersAsync(account, Option.None<RemoteItemId>(), CancellationToken);

        result.Match(_ => (object)"listed", error => error).ShouldBe(ProviderErrorFactory.CreateTransient());
    }

    [Fact]
    public async Task when_the_request_times_out_without_the_caller_cancelling_then_the_error_is_transient()
    {
        graph.Respond = _ => throw new TaskCanceledException("The request timed out.");
        var account = await AddAccountAsync();

        var result = await Browser().GetChildFoldersAsync(account, Option.None<RemoteItemId>(), CancellationToken);

        result.Match(_ => (object)"listed", error => error).ShouldBe(ProviderErrorFactory.CreateTransient());
    }

    [Fact]
    public async Task when_the_caller_cancels_then_the_listing_is_cancelled_and_logged_once()
    {
        using var cancelled = new CancellationTokenSource();
        var account = await AddAccountAsync();
        graph.Respond = _ =>
        {
            cancelled.Cancel();

            throw new OperationCanceledException(cancelled.Token);
        };

        var listing = Browser().GetChildFoldersAsync(account, Option.None<RemoteItemId>(), cancelled.Token);

        await Should.ThrowAsync<OperationCanceledException>(listing);
        logs.Logger.Messages.Count(message => message.Contains("cancelled", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
    }

    [Theory]
    [InlineData("SignInRequired", "AuthenticationRequired")]
    [InlineData("AccountNotCached", "Transient")]
    [InlineData("TokenCacheUnavailable", "Transient")]
    [InlineData("Unavailable", "Transient")]
    [InlineData("UnknownAccount", "NotFound")]
    public async Task when_no_token_can_be_had_then_the_matching_error_is_returned_and_graph_is_not_called(string outcome, string expected)
    {
        tokenSource.Outcome = outcome switch
        {
            "SignInRequired" => AccessTokenOutcomeFactory.CreateSignInRequired(),
            "AccountNotCached" => AccessTokenOutcomeFactory.CreateAccountNotCached(),
            "TokenCacheUnavailable" => AccessTokenOutcomeFactory.CreateTokenCacheUnavailable(),
            "UnknownAccount" => AccessTokenOutcomeFactory.CreateUnknownAccount(),
            _ => AccessTokenOutcomeFactory.CreateUnavailable(),
        };
        var account = await AddAccountAsync();

        var result = await Browser().GetChildFoldersAsync(account, Option.None<RemoteItemId>(), CancellationToken);

        (result.Match(_ => "listed", error => error.GetType().Name), graph.Requests.Count).ShouldBe((expected, 0));
    }

    [Fact]
    public async Task when_the_account_is_not_stored_then_it_is_not_found_and_no_token_is_asked_for()
    {
        var result = await Browser().GetChildFoldersAsync(AccountIdFactory.Create(Guid.NewGuid()), Option.None<RemoteItemId>(), CancellationToken);

        (result.Match(_ => "listed", error => error.GetType().Name), tokenSource.Calls, graph.Requests.Count).ShouldBe(("NotFound", 0, 0));
    }

    [Fact]
    public async Task when_a_listing_succeeds_or_fails_then_the_logs_carry_no_token_folder_names_or_urls()
    {
        graph.Respond = _ => GraphScript.Json(HttpStatusCode.OK, Page(Folder("f1", "Taxes 2026", 0)));
        var account = await AddAccountAsync();
        _ = await Browser().GetChildFoldersAsync(account, Option.Some(ItemId("folder-42")), CancellationToken);
        graph.Respond = _ => GraphScript.Json(HttpStatusCode.Forbidden, "{\"error\":{\"code\":\"accessDenied\",\"message\":\"Taxes 2026\"}}");
        _ = await Browser().GetChildFoldersAsync(account, Option.Some(ItemId("folder-42")), CancellationToken);

        var everything = string.Join('\n', logs.Logger.Messages.Concat(logs.Logger.Scopes.SelectMany(scope => scope.Values.Select(value => value?.ToString() ?? string.Empty))));

        (everything.Contains(Token, StringComparison.Ordinal), everything.Contains("Taxes", StringComparison.Ordinal), everything.Contains("graph.microsoft.com", StringComparison.Ordinal), everything.Contains("folder-42", StringComparison.Ordinal), everything.Contains(account.Value.ToString(), StringComparison.OrdinalIgnoreCase)).ShouldBe((false, false, false, false, false));
    }

    [Fact]
    public async Task when_the_drive_has_no_child_folders_then_an_empty_list_is_a_success()
    {
        var account = await AddAccountAsync();

        var result = await Browser().GetChildFoldersAsync(account, Option.None<RemoteItemId>(), CancellationToken);

        result.Match(folders => folders.Count, _ => -1).ShouldBe(0);
    }

    public void Dispose()
    {
        provider?.Dispose();
        graph.Dispose();
        logs.Dispose();
    }

    private static RemoteItemId ItemId(string value) => RemoteItemIdFactory.Create(value).Match(id => id, _ => throw new InvalidOperationException("The test id must be valid."));

    private static string Folder(string id, string name, int childCount) =>
        "{\"id\":\"" + id + "\",\"name\":\"" + name + "\",\"folder\":{\"childCount\":" + childCount + "},\"parentReference\":{\"id\":\"root-id\"}}";

    private static string Page(params string[] items) => "{\"value\":[" + string.Join(',', items) + "]}";

    private async Task<AccountId> AddAccountAsync()
    {
        var key = ProviderAccountKeyFactory.Create("key-1").Match(created => created, _ => throw new InvalidOperationException("The test key must be valid."));
        var registration = AccountRegistrationFactory.Create(key, CloudProviderFactory.CreateOneDrive(), "Personal", "personal@example.com", new DriveId(Drive), StorageUsageFactory.Create(1, 2));

        return (await accountStore.AddAsync(registration, CancellationToken)).ShouldBeOfType<AccountAddOutcome.Added>().AccountId;
    }

    private IRemoteFolderBrowser Browser(RetryHandlerOption? retry = null)
    {
        var services = new ServiceCollection()
            .AddLogging(logging => logging.ClearProviders().AddProvider(logs))
            .AddSingleton<TimeProvider>(time)
            .AddSingleton<IAccountStore>(accountStore)
            .AddSingleton<IAccessTokenSource>(tokenSource)
            .AddSingleton(gate)
            .AddSingleton(retry ?? new RetryHandlerOption { Delay = 0, MaxRetry = 1 })
            .AddOneDriveSync();
        _ = services.AddHttpClient(OneDriveSyncHttp.ClientName).ConfigurePrimaryHttpMessageHandler(() => graph);
        provider?.Dispose();
        provider = services.BuildServiceProvider();

        return provider.GetRequiredKeyedService<IRemoteFolderBrowser>(CloudProviderFactory.CreateOneDrive());
    }
}
