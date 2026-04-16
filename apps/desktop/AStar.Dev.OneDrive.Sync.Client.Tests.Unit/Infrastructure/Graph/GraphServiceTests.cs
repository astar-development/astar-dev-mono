using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Graph;

public sealed class GivenAGraphService
{
    [Fact]
    public void when_constructed_then_instance_is_not_null()
    {
        var service = new GraphService(Substitute.For<IUploadService>());

        _ = service.ShouldNotBeNull();
    }

    [Fact]
    public void when_constructed_then_it_implements_IGraphService()
    {
        var service = new GraphService(Substitute.For<IUploadService>());

        _ = service.ShouldBeAssignableTo<IGraphService>();
    }
}
