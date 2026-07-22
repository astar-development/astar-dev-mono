using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AStar.Dev.Velopack.Publishing.Tests.Unit;

public sealed class GivenVelopackUpdateServiceCollectionExtensions
{
    private static IServiceProvider BuildProvider(string githubRepositoryUrl)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Updates:GithubRepositoryUrl"] = githubRepositoryUrl
            })
            .Build();

        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddVelopackUpdates(configuration);

        return services.BuildServiceProvider();
    }

    [Fact]
    public void when_registered_then_options_are_bound()
    {
        var provider = BuildProvider("https://github.com/astar-development/astar-dev-mono");

        var options = provider.GetRequiredService<IOptions<VelopackUpdateSettings>>();

        options.Value.GithubRepositoryUrl.ShouldBe(new Uri("https://github.com/astar-development/astar-dev-mono"));
    }

    [Fact]
    public void when_registered_then_update_service_is_resolvable()
    {
        var provider = BuildProvider("https://github.com/astar-development/astar-dev-mono");

        var updateService = provider.GetRequiredService<IVelopackUpdateService>();

        updateService.ShouldBeOfType<VelopackUpdateService>();
    }

    [Fact]
    public void when_registered_then_update_service_is_a_singleton()
    {
        var provider = BuildProvider("https://github.com/astar-development/astar-dev-mono");

        var first = provider.GetRequiredService<IVelopackUpdateService>();
        var second = provider.GetRequiredService<IVelopackUpdateService>();

        first.ShouldBeSameAs(second);
    }
}
