using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.OneDrive;

/// <summary>The HTTP plumbing the OneDrive upload, folder and delete calls share: credentials, the drive URLs, the request pipeline with the account throttle gate, and error mapping from a response.</summary>
/// <param name="graphAccess">Finds the drive and the access token.</param>
/// <param name="httpClients">Creates the HTTP client that goes through the account throttle gate.</param>
/// <param name="timeProvider">Turns a Retry-After date into a delay.</param>
internal sealed class OneDriveTransferHttp(OneDriveGraphAccess graphAccess, IHttpClientFactory httpClients, TimeProvider timeProvider)
{
    private const string DriveRoot = "https://graph.microsoft.com/v1.0/drives/";
    private const string RootSegment = "root";
    private const string NoCode = "none";

    internal static string ParentSegment(Option<RemoteItemId> parent) => parent.Match(id => id.Value, () => RootSegment);

    internal static Uri ItemUri(TransferCredentials credentials, string itemSegment, string suffix = "") => new($"{DriveRoot}{credentials.DriveId}/items/{itemSegment}{suffix}");

    internal static Uri ChildUri(TransferCredentials credentials, string parentSegment, string name, string suffix = "") => ItemUri(credentials, $"{parentSegment}:/{Uri.EscapeDataString(name)}", suffix);

    internal static HttpRequestMessage Authorized(HttpMethod method, Uri url, TransferCredentials credentials)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credentials.AccessToken);

        return request;
    }

    internal static HttpRequestMessage Anonymous(HttpMethod method, Uri url) => new(method, url);

    internal static void AddIfMatch(HttpRequestMessage request, string tag) => request.Headers.TryAddWithoutValidation("If-Match", tag);

    internal static StringContent JsonBody(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
            write(writer);

        return new StringContent(Encoding.UTF8.GetString(stream.ToArray()), Encoding.UTF8, "application/json");
    }

    internal static string Timestamp(DateTime utc) => utc.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", System.Globalization.CultureInfo.InvariantCulture);

    internal async Task<Result<TransferCredentials, ProviderError>> CredentialsAsync(AccountId accountId, Action<string> onRefused, CancellationToken cancellationToken) =>
        await graphAccess.DriveOfAsync(accountId, cancellationToken)
            .BindAsync(drive => graphAccess.TokenOfAsync(accountId, onRefused, cancellationToken).BindAsync(token => Task.FromResult(Result.Success<TransferCredentials, ProviderError>(TransferCredentialsFactory.Create(drive, token)))))
            .ConfigureAwait(false);

    internal async Task<Result<HttpResponseMessage, ProviderError>> SendAsync(AccountId accountId, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Options.Set(OneDriveSyncHttp.AccountOption, accountId);
        using var client = httpClients.CreateClient(OneDriveSyncHttp.ClientName);
        var sending = Try.RunAsync(() => client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken), cancellationToken);
        await ((Task)sending).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        if (sending.IsCanceled)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Result.Failure<HttpResponseMessage, ProviderError>(ProviderErrorFactory.CreateTransient());
        }

        return (await sending.ConfigureAwait(false)).Match(Result.Success<HttpResponseMessage, ProviderError>, exception => Result.Failure<HttpResponseMessage, ProviderError>(GraphErrorMapper.Map(exception, timeProvider).Error));
    }

    internal async Task<(ProviderError Error, int Status, string Code)> ErrorOfAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var status = (int)response.StatusCode;
        var code = await CodeOfAsync(response, cancellationToken).ConfigureAwait(false);

        return (GraphErrorMapper.FromStatus(status, RetryAfter(response)), status, code);
    }

    private static async Task<string> CodeOfAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await Try.RunAsync(() => response.Content.ReadAsStringAsync(cancellationToken), cancellationToken).MatchAsync(text => text, _ => string.Empty).ConfigureAwait(false);
        using var document = Try.Run(() => JsonDocument.Parse(body)).Match(parsed => parsed, _ => JsonDocument.Parse("{}"));

        return document.RootElement.ValueKind is JsonValueKind.Object && document.RootElement.TryGetProperty("error", out var error) && error.ValueKind is JsonValueKind.Object && error.TryGetProperty("code", out var code) && code.ValueKind is JsonValueKind.String ? code.GetString() ?? NoCode : NoCode;
    }

    private Option<TimeSpan> RetryAfter(HttpResponseMessage response) => response.Headers.RetryAfter switch
    {
        { Delta: { } delta } => Option.Some(delta),
        { Date: { } date } => Option.Some(date - timeProvider.GetUtcNow()),
        _ => Option.None<TimeSpan>(),
    };
}
