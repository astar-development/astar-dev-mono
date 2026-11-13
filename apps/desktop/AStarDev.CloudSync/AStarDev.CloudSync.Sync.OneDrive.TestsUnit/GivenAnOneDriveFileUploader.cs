using System.Net;
using System.Security.Cryptography;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Accounts.OneDrive;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.OneDrive.TestsUnit;

public sealed class GivenAnOneDriveFileUploader : IDisposable
{
    private const string UploadUrl = "https://upload.example.test/session/secret-session-id";
    private const int Fragment = 10 * 1024 * 1024;

    private readonly OneDriveTransferFixture fixture = new();
    private readonly Progress<TransferProgress> progress = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static string ItemJson(string id = "item-1", long size = 5) => "{\"id\":\"" + id + "\",\"cTag\":\"c-" + id + "\",\"eTag\":\"e-" + id + "\",\"size\":" + size + "}";

    private static string SessionJson => "{\"uploadUrl\":\"" + UploadUrl + "\",\"expirationDateTime\":\"2026-11-11T09:00:00Z\"}";

    private static string Hex(byte[] content) => Convert.ToHexStringLower(SHA256.HashData(content));

    private static byte[] Bytes(int length) => [.. Enumerable.Range(0, length).Select(index => (byte)(index % 251))];

    private static UploadPrecondition MustNotExist => UploadPreconditionFactory.CreateMustNotExist();

    private static string Describe(Result<UploadedFile, ProviderError> result) => result.Match(uploaded => $"ok {uploaded.Id.Value} {uploaded.CTag.Value} {uploaded.ETag.Value} {uploaded.Size}", error => error.GetType().Name);

    private async Task<OneDriveTransferFixture> BuiltAsync() => await fixture.BuildAsync(CancellationToken);

    private async Task<Result<UploadedFile, ProviderError>> UploadAsync(UploadTarget target, System.IO.Abstractions.IFileInfo source, UploadPrecondition precondition, CancellationToken? token = null) =>
        await fixture.Uploader.UploadAsync(fixture.Account, target, source, precondition, progress, token ?? CancellationToken);

    private static UploadTarget Into(string parent) => UploadTargetFactory.CreateNewFileIn(Option.Some(RemoteItemIdFactory.Create(parent).Match(id => id, _ => throw new InvalidOperationException())));

    private void ScriptSimpleUpload(string itemJson)
    {
        fixture.Graph.Respond = request => request.Method.Method switch
        {
            "POST" => RecordingGraphHandler.Json(HttpStatusCode.OK, SessionJson),
            _ => RecordingGraphHandler.Json(HttpStatusCode.Created, itemJson),
        };
    }

    [Fact]
    public async Task when_a_small_new_file_is_uploaded_then_a_session_is_created_with_conflict_behavior_fail_and_the_local_write_time()
    {
        _ = await BuiltAsync();
        ScriptSimpleUpload(ItemJson());
        var source = fixture.File("a.txt", Bytes(5));
        fixture.FileSystem.File.SetLastWriteTimeUtc("/sync/a.txt", new DateTime(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc));

        var result = await UploadAsync(Into("folder-1"), source, MustNotExist);

        Describe(result).ShouldBe("ok item-1 c-item-1 e-item-1 5");
        var session = fixture.Graph.Requests[0];
        session.Method.ShouldBe(HttpMethod.Post);
        session.Url.AbsolutePath.ShouldBe($"{OneDriveTransferFixture.Items}folder-1:/a.txt:/createUploadSession");
        session.Body.ShouldContain("\"@microsoft.graph.conflictBehavior\":\"fail\"");
        session.Body.ShouldContain("\"lastModifiedDateTime\":\"2026-11-10T09:00:00.0000000Z\"");
        session.Authorization.ShouldBe($"Bearer {OneDriveTransferFixture.Token}");
    }

    [Fact]
    public async Task when_a_file_is_uploaded_to_the_drive_root_then_the_session_is_created_under_the_root_alias()
    {
        _ = await BuiltAsync();
        ScriptSimpleUpload(ItemJson());

        _ = await UploadAsync(UploadTargetFactory.CreateNewFileIn(Option.None<RemoteItemId>()), fixture.File("a.txt", Bytes(5)), MustNotExist);

        fixture.Graph.Requests[0].Url.AbsolutePath.ShouldBe($"{OneDriveTransferFixture.Items}root:/a.txt:/createUploadSession");
    }

    [Fact]
    public async Task when_a_name_has_spaces_and_non_ascii_characters_then_the_session_path_is_escaped()
    {
        _ = await BuiltAsync();
        ScriptSimpleUpload(ItemJson());

        _ = await UploadAsync(Into("folder-1"), fixture.File("Ünï code #1.txt", Bytes(5)), MustNotExist);

        fixture.Graph.Requests[0].Url.AbsoluteUri.ShouldContain("folder-1:/%C3%9C");
        fixture.Graph.Requests[0].Url.AbsoluteUri.ShouldContain("%20code%20%231.txt:/createUploadSession");
    }

    [Fact]
    public async Task when_a_file_is_replaced_then_the_session_is_created_on_the_item_with_if_match_and_never_asks_to_replace()
    {
        _ = await BuiltAsync();
        ScriptSimpleUpload(ItemJson("r1"));
        var precondition = UploadPreconditionFactory.CreateMustMatch(new ContentTag("ctag-7")).Match(found => found, _ => throw new InvalidOperationException());

        _ = await UploadAsync(UploadTargetFactory.CreateReplacingItem(RemoteItemIdFactory.Create("r1").Match(id => id, _ => throw new InvalidOperationException())), fixture.File("a.txt", Bytes(5)), precondition);

        var session = fixture.Graph.Requests[0];
        session.Url.AbsolutePath.ShouldBe($"{OneDriveTransferFixture.Items}r1/createUploadSession");
        session.Headers["If-Match"].ShouldBe("ctag-7");
        fixture.Graph.Requests.ShouldAllBe(request => !request.Body.Contains("replace", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task when_the_content_is_sent_then_the_upload_url_gets_no_bearer_token_and_the_range_covers_the_whole_file()
    {
        _ = await BuiltAsync();
        ScriptSimpleUpload(ItemJson());

        _ = await UploadAsync(Into("folder-1"), fixture.File("a.txt", Bytes(5)), MustNotExist);

        var put = fixture.Graph.Requests[1];
        put.Method.ShouldBe(HttpMethod.Put);
        put.Url.ToString().ShouldBe(UploadUrl);
        put.Authorization.ShouldBeNull();
        put.ContentRange.ShouldBe("bytes 0-4/5");
    }

    [Fact]
    public async Task when_the_file_is_uploaded_then_the_result_carries_the_sha256_of_the_content_in_the_scan_format()
    {
        _ = await BuiltAsync();
        ScriptSimpleUpload(ItemJson(size: 3000));
        var content = Bytes(3000);

        var result = await UploadAsync(Into("folder-1"), fixture.File("a.bin", content), MustNotExist);

        result.Match(uploaded => uploaded.Hash.Value, _ => "failed").ShouldBe(Hex(content));
    }

    [Fact]
    public async Task when_a_large_file_is_uploaded_then_fragments_are_ten_mebibytes_and_the_last_one_is_the_remainder()
    {
        _ = await BuiltAsync();
        var puts = 0;
        fixture.Graph.Respond = request => request.Method.Method switch
        {
            "POST" => RecordingGraphHandler.Json(HttpStatusCode.OK, SessionJson),
            _ => ++puts < 3 ? RecordingGraphHandler.Json(HttpStatusCode.Accepted, "{\"nextExpectedRanges\":[\"" + (puts * Fragment) + "-\"]}") : RecordingGraphHandler.Json(HttpStatusCode.Created, ItemJson(size: 21_000_000)),
        };
        var content = Bytes(21_000_000);

        var result = await UploadAsync(Into("folder-1"), fixture.File("big.bin", content), MustNotExist);

        fixture.Graph.Requests.Skip(1).Select(request => request.ContentRange).ShouldBe(["bytes 0-10485759/21000000", "bytes 10485760-20971519/21000000", "bytes 20971520-20999999/21000000"]);
        (Fragment % (320 * 1024)).ShouldBe(0);
        result.Match(uploaded => uploaded.Hash.Value, _ => "failed").ShouldBe(Hex(content));
    }

    [Fact]
    public async Task when_the_current_culture_is_german_then_the_content_range_still_uses_plain_invariant_numbers()
    {
        using var culture = new CultureScope("de-DE");
        _ = await BuiltAsync();
        ScriptSimpleUpload(ItemJson(size: 1_234_567));

        _ = await UploadAsync(Into("folder-1"), fixture.File("a.bin", Bytes(1_234_567)), MustNotExist);

        fixture.Graph.Requests[1].ContentRange.ShouldBe("bytes 0-1234566/1234567");
        fixture.Graph.Requests[1].Headers["Content-Length"].ShouldBe("1234567");
    }

    [Fact]
    public async Task when_a_fragment_fails_with_a_server_error_then_the_status_is_read_and_the_upload_continues_from_the_next_expected_range()
    {
        _ = await BuiltAsync();
        var puts = 0;
        fixture.Graph.Respond = request => request.Method.Method switch
        {
            "POST" => RecordingGraphHandler.Json(HttpStatusCode.OK, SessionJson),
            "GET" => RecordingGraphHandler.Json(HttpStatusCode.OK, "{\"nextExpectedRanges\":[\"" + Fragment + "-\"]}"),
            _ => ++puts switch
            {
                1 => RecordingGraphHandler.Json(HttpStatusCode.Accepted, "{\"nextExpectedRanges\":[\"" + Fragment + "-\"]}"),
                2 => RecordingGraphHandler.Json(HttpStatusCode.InternalServerError, "{}"),
                _ => RecordingGraphHandler.Json(HttpStatusCode.Created, ItemJson(size: 15_000_000)),
            },
        };

        var result = await UploadAsync(Into("folder-1"), fixture.File("big.bin", Bytes(15_000_000)), MustNotExist);

        Describe(result).ShouldStartWith("ok");
        fixture.Graph.Requests.Skip(1).Select(request => request.Method.Method + " " + request.ContentRange).ShouldBe(["PUT bytes 0-10485759/15000000", "PUT bytes 10485760-14999999/15000000", "GET ", "PUT bytes 10485760-14999999/15000000"]);
    }

    [Fact]
    public async Task when_a_fragment_is_refused_with_range_not_satisfiable_then_the_status_is_read_again_and_the_upload_continues()
    {
        _ = await BuiltAsync();
        var puts = 0;
        fixture.Graph.Respond = request => request.Method.Method switch
        {
            "POST" => RecordingGraphHandler.Json(HttpStatusCode.OK, SessionJson),
            "GET" => RecordingGraphHandler.Json(HttpStatusCode.OK, "{\"nextExpectedRanges\":[\"0-\"]}"),
            _ => ++puts == 1 ? RecordingGraphHandler.Json(HttpStatusCode.RequestedRangeNotSatisfiable, "{}") : RecordingGraphHandler.Json(HttpStatusCode.Created, ItemJson()),
        };

        var result = await UploadAsync(Into("folder-1"), fixture.File("a.txt", Bytes(5)), MustNotExist);

        Describe(result).ShouldStartWith("ok");
        fixture.Graph.Requests.Select(request => request.Method.Method).ShouldBe(["POST", "PUT", "GET", "PUT"]);
    }

    [Fact]
    public async Task when_the_session_is_gone_then_a_new_session_is_created_and_the_upload_starts_again()
    {
        _ = await BuiltAsync();
        var puts = 0;
        fixture.Graph.Respond = request => request.Method.Method switch
        {
            "POST" => RecordingGraphHandler.Json(HttpStatusCode.OK, SessionJson),
            _ => ++puts == 1 ? RecordingGraphHandler.Json(HttpStatusCode.NotFound, "{}") : RecordingGraphHandler.Json(HttpStatusCode.Created, ItemJson()),
        };

        var result = await UploadAsync(Into("folder-1"), fixture.File("a.txt", Bytes(5)), MustNotExist);

        Describe(result).ShouldStartWith("ok");
        fixture.Graph.Requests.Select(request => request.Method.Method).ShouldBe(["POST", "PUT", "POST", "PUT"]);
    }

    [Fact]
    public async Task when_the_file_changes_before_the_last_fragment_then_the_session_is_cancelled_and_the_source_is_reported_changed()
    {
        _ = await BuiltAsync();
        var source = fixture.File("big.bin", Bytes(15_000_000));
        fixture.Graph.Respond = request =>
        {
            if (request.Method == HttpMethod.Put)
                fixture.FileSystem.File.SetLastWriteTimeUtc("/sync/big.bin", new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            return request.Method.Method switch
            {
                "POST" => RecordingGraphHandler.Json(HttpStatusCode.OK, SessionJson),
                "PUT" => RecordingGraphHandler.Json(HttpStatusCode.Accepted, "{\"nextExpectedRanges\":[\"" + Fragment + "-\"]}"),
                _ => new HttpResponseMessage(HttpStatusCode.NoContent),
            };
        };

        var result = await UploadAsync(Into("folder-1"), source, MustNotExist);

        Describe(result).ShouldBe("SourceChanged");
        fixture.Graph.Requests.Select(request => request.Method.Method).ShouldBe(["POST", "PUT", "DELETE"]);
        fixture.Graph.Requests[2].Url.ToString().ShouldBe(UploadUrl);
    }

    [Fact]
    public async Task when_a_zero_byte_file_is_uploaded_then_it_is_put_with_conflict_behavior_fail_then_patched_with_the_write_time()
    {
        _ = await BuiltAsync();
        fixture.Graph.Respond = request => request.Method.Method switch
        {
            "PUT" => RecordingGraphHandler.Json(HttpStatusCode.Created, ItemJson("empty-1", 0)),
            _ => RecordingGraphHandler.Json(HttpStatusCode.OK, ItemJson("empty-1", 0).Replace("c-empty-1", "c-patched")),
        };

        var result = await UploadAsync(Into("folder-1"), fixture.File("empty.txt", []), MustNotExist);

        result.Match(uploaded => (uploaded.CTag.Value, uploaded.Size, uploaded.Hash.Value), _ => ("failed", -1L, "")).ShouldBe(("c-patched", 0L, Hex([])));
        var put = fixture.Graph.Requests[0];
        put.Method.ShouldBe(HttpMethod.Put);
        put.Url.AbsolutePath.ShouldBe($"{OneDriveTransferFixture.Items}folder-1:/empty.txt:/content");
        put.Url.Query.ShouldContain("conflictBehavior=fail");
        var patch = fixture.Graph.Requests[1];
        patch.Method.ShouldBe(HttpMethod.Patch);
        patch.Url.AbsolutePath.ShouldBe($"{OneDriveTransferFixture.Items}empty-1");
        patch.Body.ShouldContain("lastModifiedDateTime");
    }

    [Fact]
    public async Task when_a_zero_byte_file_replaces_an_item_then_the_put_carries_if_match()
    {
        _ = await BuiltAsync();
        fixture.Graph.Respond = _ => RecordingGraphHandler.Json(HttpStatusCode.OK, ItemJson("r1", 0));
        var precondition = UploadPreconditionFactory.CreateMustMatch(new ContentTag("ctag-7")).Match(found => found, _ => throw new InvalidOperationException());

        _ = await UploadAsync(UploadTargetFactory.CreateReplacingItem(RemoteItemIdFactory.Create("r1").Match(id => id, _ => throw new InvalidOperationException())), fixture.File("empty.txt", []), precondition);

        var put = fixture.Graph.Requests[0];
        put.Url.AbsolutePath.ShouldBe($"{OneDriveTransferFixture.Items}r1/content");
        put.Headers["If-Match"].ShouldBe("ctag-7");
    }

    [Theory]
    [InlineData(412, "PreconditionFailed")]
    [InlineData(409, "NameConflict")]
    [InlineData(507, "QuotaExceeded")]
    [InlineData(413, "FileTooLarge")]
    [InlineData(401, "AuthenticationRequired")]
    [InlineData(404, "NotFound")]
    [InlineData(500, "Transient")]
    public async Task when_the_session_cannot_be_created_then_the_status_is_mapped_to_a_provider_error(int status, string expected)
    {
        _ = await BuiltAsync();
        fixture.Graph.Respond = _ => RecordingGraphHandler.Json((HttpStatusCode)status, "{\"error\":{\"code\":\"x\"}}");

        var result = await UploadAsync(Into("folder-1"), fixture.File("a.txt", Bytes(5)), MustNotExist);

        Describe(result).ShouldBe(expected);
        fixture.Graph.Requests.Count.ShouldBeLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task when_the_provider_throttles_the_upload_then_the_retry_after_is_returned_and_the_account_gate_is_told()
    {
        _ = await BuiltAsync();
        fixture.Graph.Respond = _ =>
        {
            var response = RecordingGraphHandler.Json(HttpStatusCode.TooManyRequests, "{}");
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(7));

            return response;
        };

        var result = await UploadAsync(Into("folder-1"), fixture.File("a.txt", Bytes(5)), MustNotExist);

        result.Match(_ => (object)"ok", error => error).ShouldBe(ProviderErrorFactory.CreateThrottled(Option.Some(TimeSpan.FromSeconds(7))));
        fixture.Gate.Received().ReportThrottled(fixture.Account, Arg.Is<Option<TimeSpan>>(retry => retry.Equals(Option.Some(TimeSpan.FromSeconds(7)))));
    }

    [Fact]
    public async Task when_there_is_no_access_token_then_the_upload_asks_for_a_sign_in_without_calling_the_provider()
    {
        _ = await BuiltAsync();
        fixture.Tokens.Outcome = AccessTokenOutcomeFactory.CreateSignInRequired();

        var result = await UploadAsync(Into("folder-1"), fixture.File("a.txt", Bytes(5)), MustNotExist);

        Describe(result).ShouldBe("AuthenticationRequired");
        fixture.Graph.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_upload_is_cancelled_mid_transfer_then_the_session_is_deleted_without_the_cancelled_token_and_the_cancellation_is_rethrown()
    {
        _ = await BuiltAsync();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
        fixture.Graph.Respond = request =>
        {
            if (request.Method == HttpMethod.Put)
                cancellation.Cancel();

            return request.Method.Method switch
            {
                "POST" => RecordingGraphHandler.Json(HttpStatusCode.OK, SessionJson),
                "PUT" => RecordingGraphHandler.Json(HttpStatusCode.Accepted, "{\"nextExpectedRanges\":[\"" + Fragment + "-\"]}"),
                _ => new HttpResponseMessage(HttpStatusCode.NoContent),
            };
        };
        var source = fixture.File("big.bin", Bytes(15_000_000));

        var act = () => UploadAsync(Into("folder-1"), source, MustNotExist, cancellation.Token);

        await Should.ThrowAsync<OperationCanceledException>(act);
        fixture.Graph.Requests[^1].Method.ShouldBe(HttpMethod.Delete);
        fixture.Graph.Requests[^1].Url.ToString().ShouldBe(UploadUrl);
    }

    [Fact]
    public async Task when_an_upload_runs_then_neither_the_upload_url_nor_the_token_nor_the_file_name_appears_in_the_logs()
    {
        _ = await BuiltAsync();
        ScriptSimpleUpload(ItemJson());

        _ = await UploadAsync(Into("folder-1"), fixture.File("secret-name.txt", Bytes(5)), MustNotExist);

        var messages = string.Join('\n', fixture.Logs.Logger.Messages);
        messages.ShouldNotContain("secret-session-id");
        messages.ShouldNotContain(OneDriveTransferFixture.Token);
        messages.ShouldNotContain("secret-name");
        messages.ShouldNotContain("upload.example.test");
    }

    [Fact]
    public async Task when_a_failure_is_logged_then_the_log_carries_the_status_and_no_url()
    {
        _ = await BuiltAsync();
        fixture.Graph.Respond = _ => RecordingGraphHandler.Json(HttpStatusCode.InsufficientStorage, "{}");

        _ = await UploadAsync(Into("folder-1"), fixture.File("secret-name.txt", Bytes(5)), MustNotExist);

        var messages = string.Join('\n', fixture.Logs.Logger.Messages);
        messages.ShouldContain("507");
        messages.ShouldNotContain("secret-name");
    }

    public void Dispose() => fixture.Dispose();
}
