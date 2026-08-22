using System.IO.Abstractions;
using AStarDev.WallpaperScraper.Startup;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.WallpaperScraper.TestsUnit.Startup;

public sealed class GivenTheInfrastructureServiceCollectionExtensions
{
    [Fact]
    public void when_infrastructure_services_are_registered_then_the_file_system_can_be_resolved() => CreateSut().GetRequiredService<IFileSystem>().ShouldNotBeNull();

    private static ServiceProvider CreateSut() => new ServiceCollection().AddInfrastructureServices().BuildServiceProvider();
}
