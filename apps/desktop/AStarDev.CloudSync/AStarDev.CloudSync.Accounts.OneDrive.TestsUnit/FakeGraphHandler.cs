using System.Net;
using System.Text;

namespace AStarDev.CloudSync.Accounts.OneDrive.TestsUnit;

public sealed class FakeGraphHandler : HttpMessageHandler
{
    public const string DriveJson = """{ "id": "8f2a1b3c4d5e6f70", "driveType": "personal", "quota": { "used": 1500, "total": 5000 } }""";

    private readonly Lock gate = new();
    private readonly List<HttpRequestMessage> requests = [];

    public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Respond { get; init; } = (_, _) => Task.FromResult(Json(HttpStatusCode.OK, DriveJson));

    public IReadOnlyList<HttpRequestMessage> Requests
    {
        get
        {
            lock (gate)
                return [.. requests];
        }
    }

    public IReadOnlyList<string> AccessTokens => [.. Requests.Select(request => request.Headers.Authorization?.Parameter ?? string.Empty)];

    public static HttpResponseMessage Json(HttpStatusCode statusCode, string json) => new(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        lock (gate)
            requests.Add(request);

        return Respond(request, cancellationToken);
    }
}
