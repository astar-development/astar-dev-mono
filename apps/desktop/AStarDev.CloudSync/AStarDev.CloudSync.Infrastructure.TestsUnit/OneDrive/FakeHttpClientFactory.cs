namespace AStarDev.CloudSync.Infrastructure.TestsUnit.OneDrive;

public sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    private readonly List<string> requestedNames = [];

    public IReadOnlyList<string> RequestedNames => requestedNames;

    public HttpClient CreateClient(string name)
    {
        requestedNames.Add(name);

        return new HttpClient(handler, disposeHandler: false);
    }
}
