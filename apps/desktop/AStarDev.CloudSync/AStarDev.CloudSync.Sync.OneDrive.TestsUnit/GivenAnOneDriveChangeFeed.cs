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

public sealed class GivenAnOneDriveChangeFeed : IDisposable
{
    private const string Drive = "8A3F2C1D0E9B7A65";
    private const string Token = "secret-token-123";
    private const string DriveUrl = "https://graph.microsoft.com/v1.0/drives/" + Drive;
    private const string RootDelta = DriveUrl + "/root/delta";
    private const string TokenLink = RootDelta + "?token=abc123";
    private const string FunctionLink = DriveUrl + "/items/root/delta(token='abc123')";

    private readonly GraphScript graph = new();
    private readonly InMemoryAccountStore accountStore = new();
    private readonly FakeAccessTokenSource tokenSource = new();
    private readonly IAccountThrottleGate gate = Substitute.For<IAccountThrottleGate>();
    private readonly RecordingLoggerProvider logs = new();
    private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.Zero));
    private ServiceProvider? provider;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_there_is_no_cursor_then_the_drive_root_delta_is_requested_without_select_and_with_the_bearer_token()
    {
        var account = await AddAccountAsync();

        _ = await Feed().GetChangesAsync(account, Option.None<ChangeCursor>(), CancellationToken);

        var request = graph.Requests.ShouldHaveSingleItem();
        (request.Url.AbsolutePath, request.Url.Query.Contains("$select", StringComparison.Ordinal), request.Authorization).ShouldBe(($"/v1.0/drives/{Drive}/root/delta", false, $"Bearer {Token}"));
    }

    [Theory]
    [InlineData(TokenLink)]
    [InlineData(FunctionLink)]
    public async Task when_a_cursor_in_either_delta_link_form_is_given_then_exactly_that_link_is_requested(string link)
    {
        var account = await AddAccountAsync();

        _ = await Feed().GetChangesAsync(account, Option.Some(Cursor(link)), CancellationToken);

        graph.Requests.ShouldHaveSingleItem().Url.OriginalString.ShouldBe(link);
    }

    [Fact]
    public async Task when_a_feed_has_three_pages_then_each_call_reads_one_page_and_continues_from_the_returned_link()
    {
        var account = await AddAccountAsync();
        var feed = Feed();
        graph.Respond = _ => GraphScript.Json(HttpStatusCode.OK, Page("\"@odata.nextLink\":\"" + RootDelta + "?token=p2\"", Folder("f1", "One")));
        var first = await feed.GetChangesAsync(account, Option.None<ChangeCursor>(), CancellationToken);
        graph.Respond = _ => GraphScript.Json(HttpStatusCode.OK, Page("\"@odata.nextLink\":\"" + RootDelta + "?token=p3\"", Folder("f2", "Two")));
        var second = await feed.GetChangesAsync(account, Option.Some(Cursor(RootDelta + "?token=p2")), CancellationToken);
        graph.Respond = _ => GraphScript.Json(HttpStatusCode.OK, Page("\"@odata.deltaLink\":\"" + RootDelta + "?token=done\"", Folder("f3", "Three")));
        var third = await feed.GetChangesAsync(account, Option.Some(Cursor(RootDelta + "?token=p3")), CancellationToken);

        (Describe(first), Describe(second), Describe(third), graph.Requests.Count).ShouldBe(("more:1:" + RootDelta + "?token=p2", "more:1:" + RootDelta + "?token=p3", "caughtup:1:" + RootDelta + "?token=done", 3));
    }

    [Fact]
    public async Task when_items_are_returned_then_files_folders_and_deletions_are_mapped_with_their_ids_parents_names_and_tags()
    {
        graph.Respond = _ => GraphScript.Json(HttpStatusCode.OK, Page(
            "\"@odata.deltaLink\":\"" + TokenLink + "\"",
            "{\"id\":\"file-1\",\"name\":\"a.txt\",\"size\":42,\"eTag\":\"e1\",\"cTag\":\"c1\",\"lastModifiedDateTime\":\"2026-11-01T10:00:00Z\",\"parentReference\":{\"id\":\"dir-1\"},\"file\":{\"hashes\":{\"quickXorHash\":\"qx\"}}}",
            Folder("dir-1", "Docs"),
            "{\"id\":\"gone-1\",\"name\":\"old\",\"eTag\":\"e3\",\"parentReference\":{\"id\":\"dir-1\"},\"deleted\":{\"state\":\"deleted\"}}"));
        var account = await AddAccountAsync();

        var result = await Feed().GetChangesAsync(account, Option.None<ChangeCursor>(), CancellationToken);

        result.Match(page => page.Items.Select(Summary).ToArray(), _ => []).ShouldBe(["file-1|dir-1|a.txt|File|42|qx|c1|e1", "dir-1|root-id|Docs|Folder|||e2", "gone-1|dir-1|old|Deleted|||e3"]);
    }

    [Fact]
    public async Task when_a_file_has_no_content_tag_then_the_entity_tag_is_used_in_its_place()
    {
        graph.Respond = _ => GraphScript.Json(HttpStatusCode.OK, Page("\"@odata.deltaLink\":\"" + TokenLink + "\"", "{\"id\":\"file-1\",\"name\":\"a.txt\",\"size\":1,\"eTag\":\"e1\",\"parentReference\":{\"id\":\"dir-1\"},\"file\":{}}"));
        var account = await AddAccountAsync();

        var result = await Feed().GetChangesAsync(account, Option.None<ChangeCursor>(), CancellationToken);

        result.Match(page => page.Items.Select(Summary).ToArray(), _ => []).ShouldBe(["file-1|dir-1|a.txt|File|1||e1|e1"]);
    }

    [Fact]
    public async Task when_the_feed_has_the_root_notebooks_and_shared_items_then_none_of_them_are_returned()
    {
        graph.Respond = _ => GraphScript.Json(HttpStatusCode.OK, Page(
            "\"@odata.deltaLink\":\"" + TokenLink + "\"",
            "{\"id\":\"root-id\",\"name\":\"root\",\"eTag\":\"e0\",\"root\":{},\"folder\":{}}",
            "{\"id\":\"book\",\"name\":\"Notebook\",\"eTag\":\"e4\",\"folder\":{},\"package\":{\"type\":\"oneNote\"},\"parentReference\":{\"id\":\"root-id\"}}",
            "{\"id\":\"shared\",\"name\":\"Shared\",\"eTag\":\"e5\",\"folder\":{},\"remoteItem\":{\"id\":\"other\"},\"parentReference\":{\"id\":\"root-id\"}}",
            Folder("dir-1", "Docs")));
        var account = await AddAccountAsync();

        var result = await Feed().GetChangesAsync(account, Option.None<ChangeCursor>(), CancellationToken);

        result.Match(page => page.Items.Select(item => item.Id.Value).ToArray(), _ => []).ShouldBe(["dir-1"]);
    }

    [Fact]
    public async Task when_the_page_has_neither_a_next_nor_a_delta_link_then_unexpected_is_returned()
    {
        graph.Respond = _ => GraphScript.Json(HttpStatusCode.OK, Page(string.Empty, Folder("dir-1", "Docs")));
        var account = await AddAccountAsync();

        var result = await Feed().GetChangesAsync(account, Option.None<ChangeCursor>(), CancellationToken);

        result.Match(_ => "page", error => error.GetType().Name).ShouldBe("Unexpected");
    }

    [Theory]
    [InlineData("http://graph.microsoft.com/v1.0/drives/x/root/delta?token=a", "@odata.nextLink")]
    [InlineData("https://evil.example.com/v1.0/drives/x/root/delta?token=a", "@odata.nextLink")]
    [InlineData("https://evil.example.com/v1.0/drives/x/root/delta?token=a", "@odata.deltaLink")]
    public async Task when_a_returned_link_is_not_https_on_the_graph_host_then_unexpected_is_returned_instead_of_a_cursor(string link, string property)
    {
        graph.Respond = _ => GraphScript.Json(HttpStatusCode.OK, Page("\"" + property + "\":\"" + link + "\""));
        var account = await AddAccountAsync();

        var result = await Feed().GetChangesAsync(account, Option.None<ChangeCursor>(), CancellationToken);

        result.Match(_ => "page", error => error.GetType().Name).ShouldBe("Unexpected");
    }

    [Theory]
    [InlineData("http://graph.microsoft.com/v1.0/drives/x/root/delta?token=a")]
    [InlineData("https://evil.example.com/v1.0/drives/x/root/delta?token=a")]
    public async Task when_the_stored_cursor_is_not_https_on_the_graph_host_then_unexpected_is_returned_and_graph_is_not_called(string link)
    {
        var account = await AddAccountAsync();

        var result = await Feed().GetChangesAsync(account, Option.Some(Cursor(link)), CancellationToken);

        (result.Match(_ => "page", error => error.GetType().Name), graph.Requests.Count).ShouldBe(("Unexpected", 0));
    }

    [Theory]
    [InlineData("resyncChangesApplyDifferences", "ApplyDifferences")]
    [InlineData("resyncChangesUploadDifferences", "UploadDifferences")]
    [InlineData("resyncRequired", "Unknown")]
    [InlineData("somethingElse", "Unknown")]
    public async Task when_graph_answers_410_then_the_cursor_expired_with_the_resync_kind_for_that_error_code_and_the_location_cursor(string graphCode, string expectedKind)
    {
        graph.Respond = _ => Gone(graphCode, RootDelta + "?token=restart");
        var account = await AddAccountAsync();

        var result = await Feed().GetChangesAsync(account, Option.Some(Cursor(TokenLink)), CancellationToken);

        result.Match(_ => "page", error => error is ProviderError.CursorExpired expired ? $"{expired.Kind.GetType().Name}|{expired.RestartFrom.Value}" : error.GetType().Name).ShouldBe($"{expectedKind}|{RootDelta}?token=restart");
    }

    [Fact]
    public async Task when_graph_answers_410_without_a_usable_location_then_the_restart_is_a_full_enumeration_from_the_drive_root_delta()
    {
        graph.Respond = _ => Gone("resyncRequired", "https://evil.example.com/steal");
        var account = await AddAccountAsync();

        var result = await Feed().GetChangesAsync(account, Option.Some(Cursor(TokenLink)), CancellationToken);

        result.Match(_ => "page", error => error is ProviderError.CursorExpired expired ? expired.RestartFrom.Value : error.GetType().Name).ShouldBe(RootDelta);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "AuthenticationRequired")]
    [InlineData(HttpStatusCode.Forbidden, "AccessDenied")]
    [InlineData(HttpStatusCode.NotFound, "NotFound")]
    [InlineData(HttpStatusCode.InternalServerError, "Transient")]
    [InlineData(HttpStatusCode.BadRequest, "Unexpected")]
    public async Task when_graph_answers_with_another_error_status_then_it_maps_to_the_provider_error(HttpStatusCode status, string expected)
    {
        graph.Respond = _ => GraphScript.Json(status, "{\"error\":{\"code\":\"someCode\",\"message\":\"m\"}}");
        var account = await AddAccountAsync();

        var result = await Feed().GetChangesAsync(account, Option.None<ChangeCursor>(), CancellationToken);

        result.Match(_ => "page", error => error.GetType().Name).ShouldBe(expected);
    }

    [Fact]
    public async Task when_the_network_fails_then_the_error_is_transient()
    {
        graph.Respond = _ => throw new HttpRequestException("network down");
        var account = await AddAccountAsync();

        var result = await Feed().GetChangesAsync(account, Option.None<ChangeCursor>(), CancellationToken);

        result.Match(_ => "page", error => error.GetType().Name).ShouldBe("Transient");
    }

    [Fact]
    public async Task when_the_request_times_out_without_the_caller_cancelling_then_the_error_is_transient()
    {
        graph.Respond = _ => throw new TaskCanceledException("timeout");
        var account = await AddAccountAsync();

        var result = await Feed().GetChangesAsync(account, Option.None<ChangeCursor>(), CancellationToken);

        result.Match(_ => "page", error => error.GetType().Name).ShouldBe("Transient");
    }

    [Fact]
    public async Task when_the_caller_cancels_then_the_read_is_cancelled_and_logged_once()
    {
        using var cancelled = new CancellationTokenSource();
        var account = await AddAccountAsync();
        graph.Respond = _ =>
        {
            cancelled.Cancel();

            throw new OperationCanceledException(cancelled.Token);
        };

        var reading = Feed().GetChangesAsync(account, Option.None<ChangeCursor>(), cancelled.Token);

        await Should.ThrowAsync<OperationCanceledException>(reading);
        logs.Logger.Messages.Count(message => message.Contains("cancelled", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
    }

    [Theory]
    [InlineData("SignInRequired", "AuthenticationRequired")]
    [InlineData("AccountNotCached", "Transient")]
    [InlineData("UnknownAccount", "NotFound")]
    [InlineData("Unavailable", "Transient")]
    public async Task when_no_token_can_be_had_then_the_matching_error_is_returned_and_graph_is_not_called(string outcome, string expected)
    {
        tokenSource.Outcome = outcome switch
        {
            "SignInRequired" => AccessTokenOutcomeFactory.CreateSignInRequired(),
            "AccountNotCached" => AccessTokenOutcomeFactory.CreateAccountNotCached(),
            "UnknownAccount" => AccessTokenOutcomeFactory.CreateUnknownAccount(),
            _ => AccessTokenOutcomeFactory.CreateUnavailable(),
        };
        var account = await AddAccountAsync();

        var result = await Feed().GetChangesAsync(account, Option.None<ChangeCursor>(), CancellationToken);

        (result.Match(_ => "page", error => error.GetType().Name), graph.Requests.Count).ShouldBe((expected, 0));
    }

    [Fact]
    public async Task when_the_account_is_not_stored_then_it_is_not_found_and_no_token_is_asked_for()
    {
        var result = await Feed().GetChangesAsync(AccountIdFactory.Create(Guid.NewGuid()), Option.None<ChangeCursor>(), CancellationToken);

        (result.Match(_ => "page", error => error.GetType().Name), tokenSource.Calls, graph.Requests.Count).ShouldBe(("NotFound", 0, 0));
    }

    [Fact]
    public async Task when_an_incremental_feed_returns_changes_after_a_delta_link_then_the_page_is_caught_up_with_a_new_delta_cursor()
    {
        graph.Respond = _ => GraphScript.Json(HttpStatusCode.OK, Page("\"@odata.deltaLink\":\"" + RootDelta + "?token=next\"", Folder("dir-1", "Docs")));
        var account = await AddAccountAsync();

        var result = await Feed().GetChangesAsync(account, Option.Some(Cursor(TokenLink)), CancellationToken);

        Describe(result).ShouldBe("caughtup:1:" + RootDelta + "?token=next");
    }

    [Fact]
    public async Task when_graph_answers_410_without_a_location_header_then_the_restart_is_a_full_enumeration_from_the_drive_root_delta()
    {
        graph.Respond = _ => GraphScript.Json(HttpStatusCode.Gone, "{\"error\":{\"code\":\"resyncRequired\",\"message\":\"m\"}}");
        var account = await AddAccountAsync();

        var result = await Feed().GetChangesAsync(account, Option.Some(Cursor(TokenLink)), CancellationToken);

        result.Match(_ => "page", error => error is ProviderError.CursorExpired expired ? expired.RestartFrom.Value : error.GetType().Name).ShouldBe(RootDelta);
    }

    [Fact]
    public async Task when_a_deleted_item_has_no_entity_tag_then_it_is_still_reported_with_a_stand_in_tag()
    {
        graph.Respond = _ => GraphScript.Json(HttpStatusCode.OK, Page("\"@odata.deltaLink\":\"" + TokenLink + "\"", "{\"id\":\"gone-1\",\"name\":\"old\",\"parentReference\":{\"id\":\"dir-1\"},\"deleted\":{}}"));
        var account = await AddAccountAsync();

        var result = await Feed().GetChangesAsync(account, Option.None<ChangeCursor>(), CancellationToken);

        result.Match(page => page.Items.Select(Summary).ToArray(), _ => []).ShouldBe(["gone-1|dir-1|old|Deleted|||gone-1"]);
    }

    [Fact]
    public async Task when_items_have_no_kind_or_no_tags_at_all_then_they_are_left_out()
    {
        graph.Respond = _ => GraphScript.Json(HttpStatusCode.OK, Page(
            "\"@odata.deltaLink\":\"" + TokenLink + "\"",
            "{\"id\":\"odd\",\"name\":\"odd\",\"eTag\":\"e9\"}",
            "{\"id\":\"bare\",\"name\":\"bare\",\"size\":1,\"file\":{}}"));
        var account = await AddAccountAsync();

        var result = await Feed().GetChangesAsync(account, Option.None<ChangeCursor>(), CancellationToken);

        result.Match(page => page.Items.Count, _ => -1).ShouldBe(0);
    }

    [Fact]
    public async Task when_the_response_has_no_value_then_an_empty_caught_up_page_is_returned()
    {
        graph.Respond = _ => GraphScript.Json(HttpStatusCode.OK, "{\"@odata.deltaLink\":\"" + TokenLink + "\"}");
        var account = await AddAccountAsync();

        var result = await Feed().GetChangesAsync(account, Option.None<ChangeCursor>(), CancellationToken);

        Describe(result).ShouldBe("caughtup:0:" + TokenLink);
    }

    [Fact]
    public async Task when_a_request_is_made_then_it_passes_the_account_throttle_gate_for_that_account()
    {
        var account = await AddAccountAsync();

        _ = await Feed().GetChangesAsync(account, Option.None<ChangeCursor>(), CancellationToken);

        await gate.Received(1).WaitAsync(account, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_reads_succeed_or_fail_then_the_logs_carry_no_token_names_urls_cursors_or_account_id()
    {
        graph.Respond = _ => GraphScript.Json(HttpStatusCode.OK, Page("\"@odata.deltaLink\":\"" + TokenLink + "\"", Folder("dir-1", "Taxes 2026")));
        var account = await AddAccountAsync();
        _ = await Feed().GetChangesAsync(account, Option.None<ChangeCursor>(), CancellationToken);
        graph.Respond = _ => Gone("resyncRequired", RootDelta + "?token=restart");
        _ = await Feed().GetChangesAsync(account, Option.Some(Cursor(TokenLink)), CancellationToken);
        graph.Respond = _ => GraphScript.Json(HttpStatusCode.Forbidden, "{\"error\":{\"code\":\"accessDenied\",\"message\":\"Taxes 2026\"}}");
        _ = await Feed().GetChangesAsync(account, Option.None<ChangeCursor>(), CancellationToken);

        var everything = string.Join('\n', logs.Logger.Messages.Concat(logs.Logger.Scopes.SelectMany(scope => scope.Values.Select(value => value?.ToString() ?? string.Empty))));

        (everything.Contains(Token, StringComparison.Ordinal), everything.Contains("Taxes", StringComparison.Ordinal), everything.Contains("graph.microsoft.com", StringComparison.Ordinal), everything.Contains("abc123", StringComparison.Ordinal), everything.Contains(account.Value.ToString(), StringComparison.OrdinalIgnoreCase)).ShouldBe((false, false, false, false, false));
    }

    [Fact]
    public async Task when_a_page_is_read_then_the_item_count_is_logged()
    {
        graph.Respond = _ => GraphScript.Json(HttpStatusCode.OK, Page("\"@odata.deltaLink\":\"" + TokenLink + "\"", Folder("dir-1", "Docs"), Folder("dir-2", "More")));
        var account = await AddAccountAsync();

        _ = await Feed().GetChangesAsync(account, Option.None<ChangeCursor>(), CancellationToken);

        logs.Logger.Messages.ShouldContain(message => message.Contains('2', StringComparison.Ordinal) && message.Contains("item", StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        provider?.Dispose();
        graph.Dispose();
        logs.Dispose();
    }

    private static ChangeCursor Cursor(string value) => ChangeCursorFactory.Create(value).Match(cursor => cursor, _ => throw new InvalidOperationException("The test cursor must be valid."));

    private static string Folder(string id, string name) => "{\"id\":\"" + id + "\",\"name\":\"" + name + "\",\"eTag\":\"e2\",\"folder\":{\"childCount\":1},\"parentReference\":{\"id\":\"root-id\"}}";

    private static string Page(string link, params string[] items) => "{" + (link.Length > 0 ? link + "," : string.Empty) + "\"value\":[" + string.Join(',', items) + "]}";

    private static string Describe(Result<RemoteChangePage, ProviderError> result) => result.Match(
        page => page.Next switch
        {
            PageContinuation.MorePages more => $"more:{page.Items.Count}:{more.Next.Value}",
            PageContinuation.CaughtUp caughtUp => $"caughtup:{page.Items.Count}:{caughtUp.Delta.Value}",
            _ => "unknown",
        },
        error => error.GetType().Name);

    private static string Summary(RemoteItemSnapshot item) => item.Kind switch
    {
        RemoteItemKind.File file => $"{item.Id.Value}|{item.ParentId.Match(parent => parent.Value, () => string.Empty)}|{item.Name}|File|{file.Size}|{file.Hash.Match(hash => hash.Value, () => string.Empty)}|{file.CTag.Value}|{item.ETag.Value}",
        RemoteItemKind.Folder => $"{item.Id.Value}|{item.ParentId.Match(parent => parent.Value, () => string.Empty)}|{item.Name}|Folder|||{item.ETag.Value}",
        _ => $"{item.Id.Value}|{item.ParentId.Match(parent => parent.Value, () => string.Empty)}|{item.Name}|Deleted|||{item.ETag.Value}",
    };

    private static HttpResponseMessage Gone(string graphCode, string location)
    {
        var response = GraphScript.Json(HttpStatusCode.Gone, "{\"error\":{\"code\":\"" + graphCode + "\",\"message\":\"m\"}}");
        response.Headers.TryAddWithoutValidation("Location", location);

        return response;
    }

    private async Task<AccountId> AddAccountAsync()
    {
        var key = ProviderAccountKeyFactory.Create("key-1").Match(created => created, _ => throw new InvalidOperationException("The test key must be valid."));
        var registration = AccountRegistrationFactory.Create(key, CloudProviderFactory.CreateOneDrive(), "Personal", "personal@example.com", new DriveId(Drive), StorageUsageFactory.Create(1, 2));

        return (await accountStore.AddAsync(registration, CancellationToken)).ShouldBeOfType<AccountAddOutcome.Added>().AccountId;
    }

    private IRemoteChangeFeed Feed()
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

        return provider.GetRequiredKeyedService<IRemoteChangeFeed>(CloudProviderFactory.CreateOneDrive());
    }
}
