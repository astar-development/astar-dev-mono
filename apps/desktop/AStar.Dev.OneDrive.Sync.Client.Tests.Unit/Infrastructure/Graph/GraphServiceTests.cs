using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Graph;

public sealed class GraphServiceTests
{
    [Fact]
    public void Constructor_ShouldInitializeSuccessfully()
    {
        var service = new GraphService(Substitute.For<IUploadService>());

        _ = service.ShouldNotBeNull();
    }

    [Fact]
    public void GraphService_ShouldImplementIGraphService()
    {
        var service = new GraphService(Substitute.For<IUploadService>());

        _ = service.ShouldBeAssignableTo<IGraphService>();
    }
}

