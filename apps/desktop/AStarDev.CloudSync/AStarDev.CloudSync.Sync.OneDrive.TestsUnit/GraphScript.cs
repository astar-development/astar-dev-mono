using System.Net;
using System.Text;

namespace AStarDev.CloudSync.Sync.OneDrive.TestsUnit;

public sealed class GraphScript : HttpMessageHandler
{
    private readonly Lock gate = new();
    private readonly List<(Uri Url, string? Authorization)> requests = [];

    public Func<HttpRequestMessage, HttpResponseMessage> Respond { get; set; } = _ => Json(HttpStatusCode.OK, "{\"value\":[]}");

    public IReadOnlyList<(Uri Url, string? Authorization)> Requests
    {
        get
        {
            lock (gate)
                return [.. requests];
        }
    }

    public static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        lock (gate)
            requests.Add((request.RequestUri!, request.Headers.Authorization?.ToString()));

        cancellationToken.ThrowIfCancellationRequested();

        var response = Respond(request);
        response.RequestMessage = request;

        return Task.FromResult(response);
    }
}
