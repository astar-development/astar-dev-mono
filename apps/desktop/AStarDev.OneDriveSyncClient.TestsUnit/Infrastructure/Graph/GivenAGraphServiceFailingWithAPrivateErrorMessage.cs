using AStarDev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Graph;

public sealed class GivenAGraphServiceFailingWithAPrivateErrorMessage : IDisposable
{
    private const string AnyAccountId = "account-001";
    private const string AnyDriveId = "drive-001";
    private const string AnyRootId = "root-001";
    private const string AnyFolderId = "folder-001";
    private const string AnyItemId = "item-001";
    private const string AnyRemotePath = "/Documents";
    private const string AnyLocalPath = "/home/user/file.txt";
    private const string AnyDeltaLink = "https://graph.microsoft.com/v1.0/drives/drive-001/items/root/delta?token=old";
    private const string PrivateEmail = "user@example.com";
    private const string PrivatePath = "/Documents/Medical/scan.pdf";
    private const string PrivateErrorMessage = $"Access denied for {PrivateEmail} at {PrivatePath}";
    private const string OkMarker = "<ok>";

    private readonly GraphServiceTestHosts hosts = new();
    private readonly WireMockServer server;

    public GivenAGraphServiceFailingWithAPrivateErrorMessage() => server = hosts.Server;

    public void Dispose() => hosts.Dispose();

    public static TheoryData<string> MethodsCallingGraphAfterTheDriveContext =>
    [
        nameof(IGraphService.GetRootFoldersAsync),
        nameof(IGraphService.GetChildFoldersAsync),
        nameof(IGraphService.GetQuotaAsync),
        nameof(IGraphService.GetFolderIdByPathAsync),
        nameof(IGraphService.GetDownloadUrlAsync),
        nameof(IGraphService.CreateFolderAsync),
        nameof(IGraphService.DeleteItemAsync),
        nameof(IGraphService.GetLatestDeltaLinkAsync),
        nameof(IGraphService.GetDeltaChangesAsync)
    ];

    public static TheoryData<string> MethodsCatchingTokenFailures =>
    [
        nameof(IGraphService.GetDriveIdAsync),
        nameof(IGraphService.GetRootFoldersAsync),
        nameof(IGraphService.GetChildFoldersAsync),
        nameof(IGraphService.GetQuotaAsync),
        nameof(IGraphService.GetFolderIdByPathAsync),
        nameof(IGraphService.GetDownloadUrlAsync),
        nameof(IGraphService.UploadFileAsync),
        nameof(IGraphService.GetLatestDeltaLinkAsync),
        nameof(IGraphService.GetDeltaChangesAsync)
    ];

    public static TheoryData<string> AllResultMethods =>
    [
        nameof(IGraphService.GetDriveIdAsync),
        nameof(IGraphService.GetRootFoldersAsync),
        nameof(IGraphService.GetChildFoldersAsync),
        nameof(IGraphService.GetQuotaAsync),
        nameof(IGraphService.GetFolderIdByPathAsync),
        nameof(IGraphService.GetDownloadUrlAsync),
        nameof(IGraphService.UploadFileAsync),
        nameof(IGraphService.CreateFolderAsync),
        nameof(IGraphService.DeleteItemAsync),
        nameof(IGraphService.GetLatestDeltaLinkAsync),
        nameof(IGraphService.GetDeltaChangesAsync)
    ];

    [Theory]
    [MemberData(nameof(MethodsCallingGraphAfterTheDriveContext))]
    public async Task when_graph_returns_an_error_then_the_failure_names_only_the_error_type_and_status(string methodName)
    {
        StubDriveContext();
        StubEveryOtherRequestAsForbidden();

        string error = await InvokeAsync(methodName, AnyToken, TestContext.Current.CancellationToken);

        error.ShouldBe("ODataError (403)");
        error.ShouldNotContain(PrivateEmail);
        error.ShouldNotContain(PrivatePath);
    }

    [Fact]
    public async Task when_the_drive_lookup_returns_an_error_then_get_drive_id_names_only_the_error_type_and_status()
    {
        StubEveryOtherRequestAsForbidden();

        string error = await InvokeAsync(nameof(IGraphService.GetDriveIdAsync), AnyToken, TestContext.Current.CancellationToken);

        error.ShouldBe("ODataError (403)");
    }

    [Fact]
    public async Task when_the_drive_lookup_returns_an_error_then_upload_file_names_only_the_error_type_and_status()
    {
        StubEveryOtherRequestAsForbidden();

        string error = await InvokeAsync(nameof(IGraphService.UploadFileAsync), AnyToken, TestContext.Current.CancellationToken);

        error.ShouldBe("ODataError (403)");
    }

    [Fact]
    public async Task when_graph_cannot_be_reached_then_the_failure_names_only_the_exception_type()
    {
        server.Stop();

        string error = await InvokeAsync(nameof(IGraphService.GetChildFoldersAsync), AnyToken, TestContext.Current.CancellationToken);

        error.ShouldBe(nameof(HttpRequestException));
    }

    [Theory]
    [MemberData(nameof(MethodsCatchingTokenFailures))]
    public async Task when_the_token_factory_throws_then_the_failure_names_only_the_exception_type(string methodName)
    {
        string error = await InvokeAsync(methodName, _ => throw new InvalidOperationException(PrivateErrorMessage), TestContext.Current.CancellationToken);

        error.ShouldBe(nameof(InvalidOperationException));
    }

    [Theory]
    [MemberData(nameof(AllResultMethods))]
    public async Task when_the_token_factory_requires_re_authentication_then_the_exception_propagates(string methodName)
    {
        StubDriveContext();

        await Should.ThrowAsync<SyncReAuthRequiredException>(() => InvokeAsync(methodName, ThrowOnTheCallUnderTest(methodName, () => new SyncReAuthRequiredException()), TestContext.Current.CancellationToken));
    }

    [Theory]
    [MemberData(nameof(AllResultMethods))]
    public async Task when_the_call_is_cancelled_mid_flight_then_the_cancellation_propagates(string methodName)
    {
        StubDriveContext();

        await Should.ThrowAsync<OperationCanceledException>(() => InvokeAsync(methodName, ThrowOnTheCallUnderTest(methodName, () => new OperationCanceledException()), TestContext.Current.CancellationToken));
    }

    private static Task<string> AnyToken(CancellationToken cancellationToken) => Task.FromResult("any-access-token");

    private static Func<CancellationToken, Task<string>> ThrowOnTheCallUnderTest(string methodName, Func<Exception> exceptionFactory)
    {
        int tokenCallsBeforeTheCallUnderTest = ResolvesTheDriveContextBeforeCallingGraph(methodName) ? 2 : 0;
        int calls = 0;

        return _ => Interlocked.Increment(ref calls) > tokenCallsBeforeTheCallUnderTest ? throw exceptionFactory() : Task.FromResult("any-access-token");
    }

    private static bool ResolvesTheDriveContextBeforeCallingGraph(string methodName)
        => methodName is nameof(IGraphService.GetRootFoldersAsync) or nameof(IGraphService.GetQuotaAsync) or nameof(IGraphService.GetDownloadUrlAsync) or nameof(IGraphService.CreateFolderAsync) or nameof(IGraphService.DeleteItemAsync);

    private Task<string> InvokeAsync(string methodName, Func<CancellationToken, Task<string>> tokenFactory, CancellationToken cancellationToken)
    {
        var sut = hosts.CreateHost().GraphService;
        var driveId = new DriveId(AnyDriveId);

        return methodName switch
        {
            nameof(IGraphService.GetDriveIdAsync) => sut.GetDriveIdAsync(AnyAccountId, tokenFactory, cancellationToken).MatchAsync(_ => OkMarker, error => error),
            nameof(IGraphService.GetRootFoldersAsync) => sut.GetRootFoldersAsync(AnyAccountId, tokenFactory, cancellationToken).MatchAsync(_ => OkMarker, error => error),
            nameof(IGraphService.GetChildFoldersAsync) => sut.GetChildFoldersAsync(tokenFactory, driveId, AnyFolderId, cancellationToken).MatchAsync(_ => OkMarker, error => error),
            nameof(IGraphService.GetQuotaAsync) => sut.GetQuotaAsync(AnyAccountId, tokenFactory, cancellationToken).MatchAsync(_ => OkMarker, error => error),
            nameof(IGraphService.GetFolderIdByPathAsync) => sut.GetFolderIdByPathAsync(tokenFactory, driveId, AnyRemotePath, cancellationToken).MatchAsync(_ => OkMarker, error => error),
            nameof(IGraphService.GetDownloadUrlAsync) => sut.GetDownloadUrlAsync(AnyAccountId, tokenFactory, AnyItemId, cancellationToken).MatchAsync(_ => OkMarker, error => error),
            nameof(IGraphService.UploadFileAsync) => sut.UploadFileAsync(AnyAccountId, tokenFactory, AnyLocalPath, AnyRemotePath, AnyFolderId, cancellationToken).MatchAsync(_ => OkMarker, error => error),
            nameof(IGraphService.CreateFolderAsync) => sut.CreateFolderAsync(AnyAccountId, tokenFactory, AnyFolderId, "NewFolder", cancellationToken).MatchAsync(_ => OkMarker, error => error),
            nameof(IGraphService.DeleteItemAsync) => sut.DeleteItemAsync(AnyAccountId, tokenFactory, AnyItemId, cancellationToken).MatchAsync(_ => OkMarker, error => error),
            nameof(IGraphService.GetLatestDeltaLinkAsync) => sut.GetLatestDeltaLinkAsync(tokenFactory, driveId, cancellationToken).MatchAsync(_ => OkMarker, error => error),
            nameof(IGraphService.GetDeltaChangesAsync) => sut.GetDeltaChangesAsync(tokenFactory, driveId, AnyDeltaLink, cancellationToken).MatchAsync(_ => OkMarker, error => error),
            _ => throw new ArgumentOutOfRangeException(nameof(methodName), methodName, "Unknown IGraphService method.")
        };
    }

    private void StubDriveContext()
    {
        server.Given(Request.Create().WithPath("/me/drive").UsingGet()).AtPriority(1)
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBodyAsJson(new { id = AnyDriveId }));
        server.Given(Request.Create().WithPath($"/drives/{AnyDriveId}/root").UsingGet()).AtPriority(1)
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBodyAsJson(new { id = AnyRootId }));
    }

    private void StubEveryOtherRequestAsForbidden()
        => server.Given(Request.Create().UsingAnyMethod()).AtPriority(100)
            .RespondWith(Response.Create().WithStatusCode(403).WithHeader("Content-Type", "application/json").WithBodyAsJson(new { error = new { code = "accessDenied", message = PrivateErrorMessage } }));
}
