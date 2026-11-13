using System.Net;
using System.Text;

namespace AStarDev.CloudSync.Sync.OneDrive.TestsUnit;

public sealed record RecordedRequest(HttpMethod Method, Uri Url, string? Authorization, IReadOnlyDictionary<string, string> Headers, string Body, long BodyLength, string? ContentRange);

public sealed class RecordingGraphHandler : HttpMessageHandler
{
    private readonly Lock gate = new();
    private readonly List<RecordedRequest> requests = [];

    public Func<RecordedRequest, HttpResponseMessage> Respond { get; set; } = _ => Json(HttpStatusCode.OK, "{}");

    public IReadOnlyList<RecordedRequest> Requests
    {
        get
        {
            lock (gate)
                return [.. requests];
        }
    }

    public static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var body = request.Content is null ? [] : await request.Content.ReadAsByteArrayAsync(cancellationToken);
        var headers = request.Headers.Concat(request.Content?.Headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>()).ToDictionary(pair => pair.Key, pair => string.Join(',', pair.Value), StringComparer.OrdinalIgnoreCase);
        var recorded = new RecordedRequest(request.Method, request.RequestUri!, request.Headers.Authorization?.ToString(), headers, body.Length < 4096 ? Encoding.UTF8.GetString(body) : string.Empty, body.Length, request.Content?.Headers.ContentRange?.ToString());
        lock (gate)
            requests.Add(recorded);

        var response = Respond(recorded);
        response.RequestMessage = request;

        return response;
    }
}
