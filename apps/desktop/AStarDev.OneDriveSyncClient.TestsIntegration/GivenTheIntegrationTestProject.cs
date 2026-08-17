using AStarDev.OneDriveSyncClient.Infrastructure.Theme;

namespace AStarDev.OneDriveSyncClient.TestsIntegration;

public class GivenTheIntegrationTestProject
{
    [Fact]
    public void when_the_project_is_referenced_then_it_compiles() =>
        typeof(ThemeService).Assembly.GetName().Name.ShouldBe("AStarDev.OneDriveSyncClient");
}
