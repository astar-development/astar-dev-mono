using System.IO.Abstractions;
using System.Net;
using System.Net.Http.Headers;
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
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.Sync.OneDrive.TestsUnit;

public sealed class GivenAnOneDriveFileDownloader : IDisposable
{
    private const string Drive = "8A3F2C1D0E9B7A65";
    private const string ItemUrl = "https://graph.microsoft.com/v1.0/drives/" + Drive + "/items/file-1";
    private const string DownloadHost = "download.example";
    private const string DownloadUrl = "https://" + DownloadHost + "/content?sig=SECRET-SIGNATURE";
    private const string Target = "/root/Docs/a.txt";

    private static readonly byte[] Content = "0123456789"u8.ToArray();

    private readonly GraphScript graph = new();
    private readonly InMemoryAccountStore accountStore = new();
    private readonly FakeAccessTokenSource tokenSource = new();
    private readonly IAccountThrottleGate gate = Substitute.For<IAccountThrottleGate>();
    private readonly RecordingLoggerProvider logs = new();
    private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 11, 13, 9, 0, 0, TimeSpan.Zero));
    private readonly MockFileSystem fileSystem = new(options => options.SimulatingOperatingSystem(SimulationMode.Linux));
    private readonly List<HttpRequestMessage> downloads = [];
    private ServiceProvider? provider;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static string ItemJson(string? url = DownloadUrl) => "{\"id\":\"file-1\",\"name\":\"a.txt\",\"size\":10,\"cTag\":\"c-remote\",\"file\":{\"hashes\":{\"quickXorHash\":\"QXH\"}}" + (url is null ? string.Empty : ",\"@microsoft.graph.downloadUrl\":\"" + url + "\"") + "}";

    private static RemoteItemId FileId() => RemoteItemIdFactory.Create("file-1").Match(id => id, _ => throw new InvalidOperationException("The test id must be valid."));

    private static HttpResponseMessage Bytes(HttpStatusCode status, byte[] body) => new(status) { Content = new ByteArrayContent(body) };

    private void Serve(Func<HttpRequestMessage, HttpResponseMessage> download, string itemJson)
    {
        graph.Respond = request =>
        {
            if (request.RequestUri!.Host != DownloadHost)
                return GraphScript.Json(HttpStatusCode.OK, itemJson);

            lock (downloads)
                downloads.Add(request);

            return download(request);
        };
    }

    private async Task<(IRemoteFileDownloader Downloader, AccountId Account)> StartAsync()
    {
        var key = ProviderAccountKeyFactory.Create("key-1").Match(created => created, _ => throw new InvalidOperationException("The test key must be valid."));
        var registration = AccountRegistrationFactory.Create(key, CloudProviderFactory.CreateOneDrive(), "Personal", "personal@example.com", new DriveId(Drive), StorageUsageFactory.Create(1, 2));
        var account = (await accountStore.AddAsync(registration, CancellationToken)).ShouldBeOfType<AccountAddOutcome.Added>().AccountId;
        var services = new ServiceCollection()
            .AddLogging(logging => logging.ClearProviders().AddProvider(logs))
            .AddSingleton<System.TimeProvider>(time)
            .AddSingleton<IAccountStore>(accountStore)
            .AddSingleton<IAccessTokenSource>(tokenSource)
            .AddSingleton(gate)
            .AddSingleton(new RetryHandlerOption { Delay = 0, MaxRetry = 1 })
            .AddOneDriveSync();
        _ = services.AddHttpClient(OneDriveSyncHttp.ClientName).ConfigurePrimaryHttpMessageHandler(() => graph);
        provider = services.BuildServiceProvider();

        return (provider.GetRequiredKeyedService<IRemoteFileDownloader>(CloudProviderFactory.CreateOneDrive()), account);
    }

    private IFileInfo NewTarget()
    {
        _ = fileSystem.Directory.CreateDirectory("/root/Docs");

        return fileSystem.FileInfo.New(Target);
    }

    [Fact]
    public async Task when_a_file_is_downloaded_then_the_content_is_written_and_the_remote_tag_size_and_hash_come_back()
    {
        Serve(_ => Bytes(HttpStatusCode.OK, Content), ItemJson());
        var (downloader, account) = await StartAsync();
        var reported = new List<TransferProgress>();

        var result = await downloader.DownloadAsync(account, FileId(), NewTarget(), new Progress<TransferProgress>(reported.Add), CancellationToken);

        var file = result.Match(downloaded => downloaded, error => throw new InvalidOperationException($"Failed: {error.GetType().Name}"));
        file.Size.ShouldBe(10);
        file.CTag.Value.ShouldBe("c-remote");
        file.Hash.Match(hash => hash.Value, () => string.Empty).ShouldBe("QXH");
        fileSystem.File.ReadAllBytes(Target).ShouldBe(Content);
        await Task.Delay(50, CancellationToken);
        reported.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task when_the_download_url_is_fetched_then_the_bearer_token_goes_only_to_graph_and_the_url_is_never_logged()
    {
        Serve(_ => Bytes(HttpStatusCode.OK, Content), ItemJson());
        var (downloader, account) = await StartAsync();

        _ = await downloader.DownloadAsync(account, FileId(), NewTarget(), new Progress<TransferProgress>(), CancellationToken);

        graph.Requests.Where(request => request.Url.Host == DownloadHost).ShouldAllBe(request => request.Authorization == null);
        graph.Requests.Where(request => request.Url.Host != DownloadHost).ShouldAllBe(request => request.Authorization == "Bearer secret-token-123");
        logs.Logger.Messages.ShouldNotContain(message => message.Contains("SECRET-SIGNATURE", StringComparison.Ordinal) || message.Contains(DownloadHost, StringComparison.Ordinal) || message.Contains("secret-token-123", StringComparison.Ordinal));
    }

    [Fact]
    public async Task when_the_download_request_is_sent_then_it_carries_the_account_so_the_throttle_gate_applies()
    {
        Serve(_ => Bytes(HttpStatusCode.OK, Content), ItemJson());
        var (downloader, account) = await StartAsync();

        _ = await downloader.DownloadAsync(account, FileId(), NewTarget(), new Progress<TransferProgress>(), CancellationToken);

        await gate.Received().WaitAsync(account, Arg.Any<CancellationToken>());
        downloads.ShouldHaveSingleItem().Options.TryGetValue(OneDriveSyncHttp.AccountOption, out var stamped).ShouldBeTrue();
        stamped.ShouldBe(account);
    }

    [Fact]
    public async Task when_the_connection_drops_mid_file_then_the_download_resumes_with_a_range_from_the_bytes_already_written()
    {
        var attempt = 0;
        Serve(
            request =>
            {
                attempt++;
                if (attempt == 1)
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new DroppingStream(Content, 4)) };

                var part = new HttpResponseMessage(HttpStatusCode.PartialContent) { Content = new ByteArrayContent(Content[4..]) };
                part.Content.Headers.ContentRange = new ContentRangeHeaderValue(4, 9, 10);

                return part;
            },
            ItemJson());
        var (downloader, account) = await StartAsync();

        var result = await downloader.DownloadAsync(account, FileId(), NewTarget(), new Progress<TransferProgress>(), CancellationToken);

        result.Match(_ => true, _ => false).ShouldBeTrue();
        fileSystem.File.ReadAllBytes(Target).ShouldBe(Content);
        downloads.Count.ShouldBe(2);
        downloads[1].Headers.Range!.Ranges.Single().From.ShouldBe(4);
    }

    [Fact]
    public async Task when_the_server_ignores_the_range_and_sends_the_whole_file_then_the_file_is_rewritten_from_the_start()
    {
        fileSystem.Directory.CreateDirectory("/root/Docs");
        fileSystem.File.WriteAllBytes(Target, "XXXX"u8.ToArray());
        Serve(_ => Bytes(HttpStatusCode.OK, Content), ItemJson());
        var (downloader, account) = await StartAsync();

        var result = await downloader.DownloadAsync(account, FileId(), fileSystem.FileInfo.New(Target), new Progress<TransferProgress>(), CancellationToken);

        result.Match(_ => true, _ => false).ShouldBeTrue();
        fileSystem.File.ReadAllBytes(Target).ShouldBe(Content);
    }

    [Fact]
    public async Task when_the_range_is_not_satisfiable_then_the_file_already_written_counts_as_complete()
    {
        fileSystem.Directory.CreateDirectory("/root/Docs");
        fileSystem.File.WriteAllBytes(Target, Content);
        Serve(_ => new HttpResponseMessage(HttpStatusCode.RequestedRangeNotSatisfiable), ItemJson());
        var (downloader, account) = await StartAsync();

        var result = await downloader.DownloadAsync(account, FileId(), fileSystem.FileInfo.New(Target), new Progress<TransferProgress>(), CancellationToken);

        result.Match(file => file.Size, _ => -1).ShouldBe(10);
        fileSystem.File.ReadAllBytes(Target).ShouldBe(Content);
    }

    [Fact]
    public async Task when_the_connection_keeps_dropping_then_it_gives_up_after_three_attempts_with_a_transient_error()
    {
        Serve(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new DroppingStream(Content, 0)) }, ItemJson());
        var (downloader, account) = await StartAsync();

        var result = await downloader.DownloadAsync(account, FileId(), NewTarget(), new Progress<TransferProgress>(), CancellationToken);

        result.Match(_ => "ok", error => error.GetType().Name).ShouldBe("Transient");
        downloads.Count.ShouldBe(3);
    }

    [Fact]
    public async Task when_the_download_url_answers_not_found_then_the_error_is_not_found_and_nothing_is_retried()
    {
        Serve(_ => new HttpResponseMessage(HttpStatusCode.NotFound), ItemJson());
        var (downloader, account) = await StartAsync();

        var result = await downloader.DownloadAsync(account, FileId(), NewTarget(), new Progress<TransferProgress>(), CancellationToken);

        result.Match(_ => "ok", error => error.GetType().Name).ShouldBe("NotFound");
        downloads.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_item_has_no_download_url_then_the_error_is_not_found()
    {
        Serve(_ => Bytes(HttpStatusCode.OK, Content), ItemJson(url: null));
        var (downloader, account) = await StartAsync();

        var result = await downloader.DownloadAsync(account, FileId(), NewTarget(), new Progress<TransferProgress>(), CancellationToken);

        result.Match(_ => "ok", error => error.GetType().Name).ShouldBe("NotFound");
        downloads.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_download_is_throttled_then_the_error_is_throttled_with_the_retry_after_and_is_not_retried_here()
    {
        Serve(
            _ =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));

                return response;
            },
            ItemJson());
        var (downloader, account) = await StartAsync();

        var result = await downloader.DownloadAsync(account, FileId(), NewTarget(), new Progress<TransferProgress>(), CancellationToken);

        result.Match(_ => TimeSpan.Zero, error => error is ProviderError.Throttled throttled ? throttled.RetryAfter.Match(delay => delay, () => TimeSpan.Zero) : TimeSpan.MinValue).ShouldBe(TimeSpan.FromSeconds(30));
        downloads.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_download_url_answers_with_a_server_error_then_it_is_retried_and_then_succeeds()
    {
        var attempt = 0;
        Serve(_ => ++attempt == 1 ? new HttpResponseMessage(HttpStatusCode.InternalServerError) : Bytes(HttpStatusCode.OK, Content), ItemJson());
        var (downloader, account) = await StartAsync();

        var result = await downloader.DownloadAsync(account, FileId(), NewTarget(), new Progress<TransferProgress>(), CancellationToken);

        result.Match(_ => true, _ => false).ShouldBeTrue();
        downloads.Count.ShouldBe(2);
    }

    [Fact]
    public async Task when_the_account_needs_to_sign_in_then_the_error_is_authentication_required_and_no_request_is_made()
    {
        tokenSource.Outcome = AccessTokenOutcomeFactory.CreateSignInRequired();
        Serve(_ => Bytes(HttpStatusCode.OK, Content), ItemJson());
        var (downloader, account) = await StartAsync();

        var result = await downloader.DownloadAsync(account, FileId(), NewTarget(), new Progress<TransferProgress>(), CancellationToken);

        result.Match(_ => "ok", error => error.GetType().Name).ShouldBe("AuthenticationRequired");
        graph.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_item_lookup_fails_then_the_graph_error_is_mapped()
    {
        graph.Respond = _ => GraphScript.Json(HttpStatusCode.NotFound, "{\"error\":{\"code\":\"itemNotFound\"}}");
        var (downloader, account) = await StartAsync();

        var result = await downloader.DownloadAsync(account, FileId(), NewTarget(), new Progress<TransferProgress>(), CancellationToken);

        result.Match(_ => "ok", error => error.GetType().Name).ShouldBe("NotFound");
    }

    [Fact]
    public async Task when_the_caller_cancels_then_the_cancellation_propagates()
    {
        Serve(_ => Bytes(HttpStatusCode.OK, Content), ItemJson());
        var (downloader, account) = await StartAsync();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => downloader.DownloadAsync(account, FileId(), NewTarget(), new Progress<TransferProgress>(), cancellation.Token));
    }

    [Fact]
    public void when_the_module_is_added_then_the_file_downloader_is_registered_keyed_by_onedrive()
    {
        var services = new ServiceCollection().AddOneDriveSync();

        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(IRemoteFileDownloader) && descriptor.IsKeyedService && Equals(descriptor.ServiceKey, CloudProviderFactory.CreateOneDrive()) && descriptor.Lifetime == ServiceLifetime.Singleton);
    }

    public void Dispose()
    {
        provider?.Dispose();
        graph.Dispose();
        logs.Dispose();
    }
}
