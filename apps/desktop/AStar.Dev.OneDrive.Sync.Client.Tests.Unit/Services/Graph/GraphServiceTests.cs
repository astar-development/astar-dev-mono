using AStar.Dev.OneDrive.Sync.Client.Services.Graph;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Graph;

public class GraphServiceTests
{
    [Fact]
    public void Constructor_ShouldInitializeSuccessfully()
    {
        var service = new GraphService();

        _ = service.ShouldNotBeNull();
    }

    [Fact]
    public void GraphService_ShouldImplementIGraphService()
    {
        var service = new GraphService();

        _ = service.ShouldBeAssignableTo<IGraphService>();
    }
}

