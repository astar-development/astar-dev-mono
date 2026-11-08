using System.Net;
using System.Text;

namespace AStarDev.CloudSync.Accounts.OneDrive.TestsUnit;

public sealed class FakeGraphHandler : HttpMessageHandler
{
    private readonly List<HttpRequestMessage> requests = [];

    public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Respond { get; init; } = (_, _) => Task.FromResult(Json(HttpStatusCode.OK, "{}"));

    public IReadOnlyList<HttpRequestMessage> Requests => requests;

    public static HttpResponseMessage Json(HttpStatusCode statusCode, string json) => new(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        requests.Add(request);

        return Respond(request, cancellationToken);
    }
}
