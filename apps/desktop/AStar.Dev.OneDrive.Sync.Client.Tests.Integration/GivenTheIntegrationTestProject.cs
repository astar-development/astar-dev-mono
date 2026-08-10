using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Theme;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Integration;

public class GivenTheIntegrationTestProject
{
    [Fact]
    public void when_the_project_is_referenced_then_it_compiles() =>
        typeof(ThemeService).Assembly.GetName().Name.ShouldBe("AStar.Dev.OneDrive.Sync.Client");
}
