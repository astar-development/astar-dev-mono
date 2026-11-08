namespace AStarDev.CloudSync.Accounts.OneDrive.TestsUnit;

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
