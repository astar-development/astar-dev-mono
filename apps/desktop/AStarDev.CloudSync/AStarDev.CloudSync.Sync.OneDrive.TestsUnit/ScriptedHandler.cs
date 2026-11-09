using System.Net;

namespace AStarDev.CloudSync.Sync.OneDrive.TestsUnit;

public sealed class ScriptedHandler : HttpMessageHandler
{
    private int requests;

    public Func<HttpResponseMessage> Next { get; set; } = () => new HttpResponseMessage(HttpStatusCode.OK);

    public int Requests => requests;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref requests);

        return Task.FromResult(Next());
    }
}
