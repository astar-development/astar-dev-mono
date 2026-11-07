namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Graph;

internal sealed class GraphServiceTestHosts : IDisposable
{
    private readonly List<GraphServiceTestHost> hosts = [];

    public WireMockServer Server { get; } = WireMockServer.Start();

    public GraphServiceTestHost CreateHost()
    {
        var host = new GraphServiceTestHost(Server);
        hosts.Add(host);

        return host;
    }

    public void Dispose()
    {
        foreach (var host in hosts)
            host.Dispose();

        Server.Stop();
        Server.Dispose();
    }
}
