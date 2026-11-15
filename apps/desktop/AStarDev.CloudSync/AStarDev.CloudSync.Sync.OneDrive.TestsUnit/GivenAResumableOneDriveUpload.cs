using System.IO.Abstractions;
using System.Net;
using System.Security.Cryptography;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.OneDrive.TestsUnit;

public sealed class GivenAResumableOneDriveUpload : IDisposable
{
    private const string UploadUrl = "https://upload.example.test/session/secret-session-id";
    private const string Target = "new:folder-1/big.bin";
    private const long Threshold = UploadSessionLimits.PersistedAboveBytes;
    private const long ConfirmedByRestart = Threshold;

    private static readonly Lazy<byte[]> Large = new(() => Filled(Threshold + 1));

    private readonly OneDriveTransferFixture fixture = new();
    private readonly Progress<TransferProgress> progress = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static string SessionJson(string expires = "2026-11-11T09:00:00Z") => "{\"uploadUrl\":\"" + UploadUrl + "\",\"expirationDateTime\":\"" + expires + "\"}";

    private static string ItemJson(long size) => "{\"id\":\"item-1\",\"cTag\":\"c-1\",\"eTag\":\"e-1\",\"size\":" + size + "}";

    private static byte[] Filled(long length)
    {
        var buffer = new byte[length];
        for (long index = 0; index < length; index++)
            buffer[index] = (byte)(index % 251);

        return buffer;
    }

    private static string Hex(byte[] content) => Convert.ToHexStringLower(SHA256.HashData(content));

    private static UploadTarget Into(string parent) => UploadTargetFactory.CreateNewFileIn(Option.Some(RemoteItemIdFactory.Create(parent).Match(id => id, _ => throw new InvalidOperationException())));

    private static string Describe(Result<UploadedFile, ProviderError> result) => result.Match(uploaded => "ok " + uploaded.Hash.Value, error => error.GetType().Name);

    private static HttpResponseMessage FragmentResponse(RecordedRequest request, long length)
    {
        var end = long.Parse(request.ContentRange!.Split('-')[1].Split('/')[0]);

        return end + 1 == length
            ? RecordingGraphHandler.Json(HttpStatusCode.Created, ItemJson(length))
            : RecordingGraphHandler.Json(HttpStatusCode.Accepted, "{\"nextExpectedRanges\":[\"" + (end + 1) + "-\"]}");
    }

    private IFileInfo LargeFile(byte[]? content = null) => fixture.File("big.bin", content ?? Large.Value);

    private UploadSession Saved(IFileInfo source, DateTimeOffset? expires = null, long? length = null, DateTime? writeUtc = null, string target = Target)
    {
        source.Refresh();

        return UploadSessionFactory.Create(new Uri(UploadUrl), expires ?? fixture.Time.GetUtcNow().AddHours(1), length ?? source.Length, writeUtc ?? source.LastWriteTimeUtc, ConfirmedByRestart, target);
    }

    private async Task<Result<UploadedFile, ProviderError>> UploadAsync(IFileInfo source, CancellationToken? token = null) =>
        await fixture.Uploader.UploadAsync(fixture.Account, Into("folder-1"), source, UploadPreconditionFactory.CreateMustNotExist(), progress, token ?? CancellationToken);

    private void ScriptFreshSession(long length)
    {
        fixture.Graph.Respond = request => request.Method.Method switch
        {
            "POST" => RecordingGraphHandler.Json(HttpStatusCode.OK, SessionJson()),
            "DELETE" => new HttpResponseMessage(HttpStatusCode.NoContent),
            _ => FragmentResponse(request, length),
        };
    }

    private void ScriptResumeFrom(long next, long length)
    {
        fixture.Graph.Respond = request => request.Method.Method switch
        {
            "GET" => RecordingGraphHandler.Json(HttpStatusCode.OK, "{\"nextExpectedRanges\":[\"" + next + "-\"]}"),
            "POST" => RecordingGraphHandler.Json(HttpStatusCode.OK, SessionJson()),
            "DELETE" => new HttpResponseMessage(HttpStatusCode.NoContent),
            _ => FragmentResponse(request, length),
        };
    }

    [Fact]
    public async Task when_a_file_over_the_threshold_starts_uploading_then_its_session_is_saved_and_forgotten_once_the_upload_completes()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        var source = LargeFile();
        ScriptFreshSession(source.Length);
        var savedWhileUploading = 0;
        var original = fixture.Graph.Respond;
        fixture.Graph.Respond = request =>
        {
            if (request.Method == HttpMethod.Put)
                savedWhileUploading = Math.Max(savedWhileUploading, fixture.UploadSessions.Sessions.Count);

            return original(request);
        };

        var result = await UploadAsync(source);

        Describe(result).ShouldBe("ok " + Hex(Large.Value));
        savedWhileUploading.ShouldBe(1);
        fixture.UploadSessions.Sessions.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_fragment_is_confirmed_then_the_saved_session_carries_the_confirmed_bytes_the_size_and_the_expiry()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        var source = LargeFile();
        UploadSession? seen = null;
        var puts = 0;
        fixture.Graph.Respond = request =>
        {
            if (request.Method == HttpMethod.Put && ++puts == 2)
                seen = fixture.UploadSessions.Sessions.Values.Single();

            return request.Method.Method switch
            {
                "POST" => RecordingGraphHandler.Json(HttpStatusCode.OK, SessionJson()),
                _ => FragmentResponse(request, source.Length),
            };
        };

        _ = await UploadAsync(source);

        seen.ShouldNotBeNull();
        seen.ConfirmedBytes.ShouldBe(10 * 1024 * 1024);
        seen.SourceLength.ShouldBe(source.Length);
        seen.ExpiresAt.ShouldBe(DateTimeOffset.Parse("2026-11-11T09:00:00Z"));
        seen.Url.ToString().ShouldBe(UploadUrl);
    }

    [Fact]
    public async Task when_a_file_is_exactly_at_the_threshold_then_no_session_is_saved()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        var source = LargeFile(Filled(Threshold));
        ScriptFreshSession(source.Length);

        var result = await UploadAsync(source);

        Describe(result).ShouldStartWith("ok");
        fixture.UploadSessions.Saves.ShouldBe(0);
    }

    [Fact]
    public async Task when_a_small_file_is_uploaded_then_no_session_is_saved()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        var source = fixture.File("small.bin", Filled(15_000_000));
        ScriptFreshSession(15_000_000);

        _ = await UploadAsync(source);

        fixture.UploadSessions.Saves.ShouldBe(0);
    }

    [Fact]
    public async Task when_a_valid_session_is_saved_for_an_unchanged_file_then_the_upload_continues_from_the_confirmed_bytes_without_a_new_session()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        var source = LargeFile();
        fixture.UploadSessions.Seed(fixture.Account, source, Saved(source));
        ScriptResumeFrom(ConfirmedByRestart, source.Length);

        var result = await UploadAsync(source);

        Describe(result).ShouldBe("ok " + Hex(Large.Value));
        fixture.Graph.Requests.Select(request => request.Method.Method + " " + request.ContentRange).ShouldBe(["GET ", $"PUT bytes {ConfirmedByRestart}-{ConfirmedByRestart}/{source.Length}"]);
        fixture.Graph.Requests[0].Url.ToString().ShouldBe(UploadUrl);
        fixture.UploadSessions.Sessions.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_provider_has_confirmed_fewer_bytes_than_saved_then_the_upload_continues_from_the_providers_offset()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        var source = LargeFile();
        fixture.UploadSessions.Seed(fixture.Account, source, Saved(source));
        var providerOffset = Threshold - (10L * 1024 * 1024);
        ScriptResumeFrom(providerOffset, source.Length);

        _ = await UploadAsync(source);

        fixture.Graph.Requests[1].ContentRange.ShouldBe($"bytes {providerOffset}-{providerOffset + (10L * 1024 * 1024) - 1}/{source.Length}");
    }

    [Fact]
    public async Task when_the_saved_session_has_expired_then_it_is_deleted_and_the_upload_starts_again_from_zero()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        var source = LargeFile();
        fixture.UploadSessions.Seed(fixture.Account, source, Saved(source, expires: fixture.Time.GetUtcNow().AddMinutes(-1)));
        ScriptFreshSession(source.Length);

        var result = await UploadAsync(source);

        Describe(result).ShouldStartWith("ok");
        fixture.Graph.Requests.Select(request => request.Method.Method).Take(3).ShouldBe(["DELETE", "POST", "PUT"]);
        fixture.Graph.Requests[2].ContentRange.ShouldStartWith("bytes 0-");
        fixture.UploadSessions.Removals.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task when_the_saved_session_has_only_seconds_left_then_it_is_deleted_and_the_upload_starts_again()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        var source = LargeFile();
        fixture.UploadSessions.Seed(fixture.Account, source, Saved(source, expires: fixture.Time.GetUtcNow().AddSeconds(30)));
        ScriptFreshSession(source.Length);

        _ = await UploadAsync(source);

        fixture.Graph.Requests.Select(request => request.Method.Method).Take(3).ShouldBe(["DELETE", "POST", "PUT"]);
    }

    [Fact]
    public async Task when_the_provider_reports_no_ranges_left_then_the_saved_session_is_deleted_and_the_upload_starts_again()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        var source = LargeFile();
        fixture.UploadSessions.Seed(fixture.Account, source, Saved(source));
        fixture.Graph.Respond = request => request.Method.Method switch
        {
            "GET" => RecordingGraphHandler.Json(HttpStatusCode.OK, "{\"nextExpectedRanges\":[]}"),
            "POST" => RecordingGraphHandler.Json(HttpStatusCode.OK, SessionJson()),
            "DELETE" => new HttpResponseMessage(HttpStatusCode.NoContent),
            _ => FragmentResponse(request, source.Length),
        };

        _ = await UploadAsync(source);

        fixture.Graph.Requests.Select(request => request.Method.Method).Take(4).ShouldBe(["GET", "DELETE", "POST", "PUT"]);
    }

    [Fact]
    public async Task when_the_provider_reports_an_offset_at_the_end_of_the_file_then_the_saved_session_is_deleted_and_the_upload_starts_again()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        var source = LargeFile();
        fixture.UploadSessions.Seed(fixture.Account, source, Saved(source));
        ScriptResumeFrom(source.Length, source.Length);

        _ = await UploadAsync(source);

        fixture.Graph.Requests.Select(request => request.Method.Method).Take(4).ShouldBe(["GET", "DELETE", "POST", "PUT"]);
    }

    [Fact]
    public async Task when_the_provider_gives_no_expiry_then_no_session_is_saved()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        var source = LargeFile();
        fixture.Graph.Respond = request => request.Method.Method switch
        {
            "POST" => RecordingGraphHandler.Json(HttpStatusCode.OK, "{\"uploadUrl\":\"" + UploadUrl + "\"}"),
            _ => FragmentResponse(request, source.Length),
        };

        var result = await UploadAsync(source);

        Describe(result).ShouldStartWith("ok");
        fixture.UploadSessions.Saves.ShouldBe(0);
    }

    [Fact]
    public async Task when_the_file_changed_in_size_then_the_old_session_is_deleted_and_the_upload_restarts()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        var source = LargeFile();
        fixture.UploadSessions.Seed(fixture.Account, source, Saved(source, length: source.Length - 1));
        ScriptFreshSession(source.Length);

        var result = await UploadAsync(source);

        Describe(result).ShouldStartWith("ok");
        fixture.Graph.Requests.Select(request => request.Method.Method).Take(3).ShouldBe(["DELETE", "POST", "PUT"]);
        fixture.UploadSessions.Removals.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task when_the_file_changed_in_write_time_then_the_old_session_is_deleted_and_the_upload_restarts()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        var source = LargeFile();
        fixture.UploadSessions.Seed(fixture.Account, source, Saved(source, writeUtc: source.LastWriteTimeUtc.AddSeconds(-5)));
        ScriptFreshSession(source.Length);

        _ = await UploadAsync(source);

        fixture.Graph.Requests.Select(request => request.Method.Method).Take(3).ShouldBe(["DELETE", "POST", "PUT"]);
        fixture.Graph.Requests.Count(request => request.Method == HttpMethod.Get).ShouldBe(0);
    }

    [Fact]
    public async Task when_the_saved_session_was_for_another_destination_then_it_is_not_reused()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        var source = LargeFile();
        fixture.UploadSessions.Seed(fixture.Account, source, Saved(source, target: "new:other-folder/big.bin"));
        ScriptFreshSession(source.Length);

        _ = await UploadAsync(source);

        fixture.Graph.Requests.Select(request => request.Method.Method).Take(3).ShouldBe(["DELETE", "POST", "PUT"]);
    }

    [Fact]
    public async Task when_the_provider_no_longer_knows_the_saved_session_then_a_new_session_is_created_and_the_old_one_forgotten()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        var source = LargeFile();
        fixture.UploadSessions.Seed(fixture.Account, source, Saved(source));
        fixture.Graph.Respond = request => request.Method.Method switch
        {
            "GET" => RecordingGraphHandler.Json(HttpStatusCode.NotFound, "{}"),
            "POST" => RecordingGraphHandler.Json(HttpStatusCode.OK, SessionJson()),
            _ => FragmentResponse(request, source.Length),
        };

        var result = await UploadAsync(source);

        Describe(result).ShouldStartWith("ok");
        fixture.Graph.Requests.Select(request => request.Method.Method).Take(3).ShouldBe(["GET", "POST", "PUT"]);
        fixture.Graph.Requests[2].ContentRange.ShouldStartWith("bytes 0-");
    }

    [Fact]
    public async Task when_the_saved_session_cannot_be_read_then_the_upload_starts_fresh()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        var source = LargeFile();
        fixture.UploadSessions.Fails = true;
        ScriptFreshSession(source.Length);

        var result = await UploadAsync(source);

        Describe(result).ShouldStartWith("ok");
        fixture.Graph.Requests[0].Method.ShouldBe(HttpMethod.Post);
    }

    [Fact]
    public async Task when_a_large_upload_is_cancelled_then_the_saved_session_is_kept_and_not_deleted_at_the_provider()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        var source = LargeFile();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
        fixture.Graph.Respond = request =>
        {
            if (request.Method == HttpMethod.Put)
                cancellation.Cancel();

            return request.Method.Method switch
            {
                "POST" => RecordingGraphHandler.Json(HttpStatusCode.OK, SessionJson()),
                _ => RecordingGraphHandler.Json(HttpStatusCode.Accepted, "{\"nextExpectedRanges\":[\"10485760-\"]}"),
            };
        };

        var act = () => UploadAsync(source, cancellation.Token);

        await Should.ThrowAsync<OperationCanceledException>(act);
        fixture.UploadSessions.Sessions.Count.ShouldBe(1);
        fixture.Graph.Requests.ShouldNotContain(request => request.Method == HttpMethod.Delete);
    }

    [Fact]
    public async Task when_a_large_upload_runs_out_of_retries_then_the_saved_session_is_kept_and_not_deleted_at_the_provider()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        var source = LargeFile();
        fixture.Graph.Respond = request => request.Method.Method switch
        {
            "POST" => RecordingGraphHandler.Json(HttpStatusCode.OK, SessionJson()),
            "GET" => RecordingGraphHandler.Json(HttpStatusCode.OK, "{\"nextExpectedRanges\":[\"0-\"]}"),
            _ => RecordingGraphHandler.Json(HttpStatusCode.InternalServerError, "{}"),
        };

        var result = await UploadAsync(source);

        Describe(result).ShouldBe("Transient");
        fixture.UploadSessions.Sessions.Count.ShouldBe(1);
        fixture.Graph.Requests.ShouldNotContain(request => request.Method == HttpMethod.Delete);
    }

    [Fact]
    public async Task when_the_provider_cannot_be_reached_for_the_status_then_the_saved_session_is_kept_for_the_next_attempt()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        var source = LargeFile();
        fixture.UploadSessions.Seed(fixture.Account, source, Saved(source));
        fixture.Graph.Respond = _ => RecordingGraphHandler.Json(HttpStatusCode.InternalServerError, "{}");

        var result = await UploadAsync(source);

        Describe(result).ShouldBe("Transient");
        fixture.UploadSessions.Sessions.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_a_resumed_upload_finds_the_name_taken_then_it_fails_with_a_name_conflict_and_forgets_the_session()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        var source = LargeFile();
        fixture.UploadSessions.Seed(fixture.Account, source, Saved(source));
        fixture.Graph.Respond = request => request.Method.Method switch
        {
            "GET" => RecordingGraphHandler.Json(HttpStatusCode.OK, "{\"nextExpectedRanges\":[\"" + ConfirmedByRestart + "-\"]}"),
            "PUT" => RecordingGraphHandler.Json(HttpStatusCode.Conflict, "{\"error\":{\"code\":\"nameAlreadyExists\"}}"),
            _ => new HttpResponseMessage(HttpStatusCode.NoContent),
        };

        var result = await UploadAsync(source);

        Describe(result).ShouldBe("NameConflict");
        fixture.UploadSessions.Sessions.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_resumed_upload_finds_the_item_changed_by_someone_else_then_it_fails_with_a_precondition_error_and_forgets_the_session()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        var source = LargeFile();
        fixture.UploadSessions.Seed(fixture.Account, source, Saved(source));
        fixture.Graph.Respond = request => request.Method.Method switch
        {
            "GET" => RecordingGraphHandler.Json(HttpStatusCode.OK, "{\"nextExpectedRanges\":[\"" + ConfirmedByRestart + "-\"]}"),
            "PUT" => RecordingGraphHandler.Json(HttpStatusCode.PreconditionFailed, "{}"),
            _ => new HttpResponseMessage(HttpStatusCode.NoContent),
        };

        var result = await UploadAsync(source);

        Describe(result).ShouldBe("PreconditionFailed");
        fixture.UploadSessions.Sessions.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_same_saved_session_is_resumed_by_two_passes_at_once_then_one_wins_the_other_conflicts_and_nothing_is_left_saved()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        var source = LargeFile();
        fixture.UploadSessions.Seed(fixture.Account, source, Saved(source));
        var puts = 0;
        fixture.Graph.Respond = request => request.Method.Method switch
        {
            "GET" => RecordingGraphHandler.Json(HttpStatusCode.OK, "{\"nextExpectedRanges\":[\"" + ConfirmedByRestart + "-\"]}"),
            "PUT" => Interlocked.Increment(ref puts) == 1 ? RecordingGraphHandler.Json(HttpStatusCode.Created, ItemJson(source.Length)) : RecordingGraphHandler.Json(HttpStatusCode.Conflict, "{\"error\":{\"code\":\"nameAlreadyExists\"}}"),
            _ => new HttpResponseMessage(HttpStatusCode.NoContent),
        };

        var results = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Task.Run(() => UploadAsync(source), CancellationToken)));

        results.Select(Describe).Order().ShouldBe(["NameConflict", "ok " + Hex(Large.Value)]);
        fixture.UploadSessions.Sessions.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_resumed_upload_is_cancelled_then_the_provider_session_and_the_saved_session_are_kept()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        var source = LargeFile();
        fixture.UploadSessions.Seed(fixture.Account, source, Saved(source));
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
        fixture.Graph.Respond = request =>
        {
            if (request.Method == HttpMethod.Put)
                cancellation.Cancel();

            return request.Method.Method switch
            {
                "GET" => RecordingGraphHandler.Json(HttpStatusCode.OK, "{\"nextExpectedRanges\":[\"" + ConfirmedByRestart + "-\"]}"),
                "PUT" => RecordingGraphHandler.Json(HttpStatusCode.Accepted, "{\"nextExpectedRanges\":[\"" + ConfirmedByRestart + "-\"]}"),
                _ => new HttpResponseMessage(HttpStatusCode.NoContent),
            };
        };

        var act = () => UploadAsync(source, cancellation.Token);

        await Should.ThrowAsync<OperationCanceledException>(act);
        fixture.Graph.Requests.ShouldNotContain(request => request.Method == HttpMethod.Delete);
        fixture.UploadSessions.Sessions.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_a_saved_session_is_resumed_then_neither_the_upload_url_nor_the_file_name_appears_in_the_logs()
    {
        _ = await fixture.BuildAsync(CancellationToken);
        var source = LargeFile();
        fixture.UploadSessions.Seed(fixture.Account, source, Saved(source));
        ScriptResumeFrom(ConfirmedByRestart, source.Length);

        _ = await UploadAsync(source);

        var messages = string.Join('\n', fixture.Logs.Logger.Messages);
        messages.ShouldContain(ConfirmedByRestart.ToString(System.Globalization.CultureInfo.InvariantCulture));
        messages.ShouldNotContain("secret-session-id");
        messages.ShouldNotContain("upload.example.test");
        messages.ShouldNotContain("big.bin");
    }

    [Fact]
    public void when_a_session_is_printed_then_the_upload_address_is_not_shown()
    {
        var session = UploadSessionFactory.Create(new Uri(UploadUrl), DateTimeOffset.UnixEpoch, 5, DateTime.UnixEpoch, 9, Target);

        session.ToString().ShouldNotContain("secret-session-id");
        session.ConfirmedBytes.ShouldBe(5);
    }

    public void Dispose() => fixture.Dispose();
}
