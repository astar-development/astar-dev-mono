using AStar.Dev.OneDrive.Sync.Client.Infrastructure.ApplicationConfiguration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.ApplicationConfiguration;

public sealed class GivenUpdateSettings
{
    private static IOptions<UpdateSettings> BuildOptions(string githubRepositoryUrl)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Updates:GithubRepositoryUrl"] = githubRepositoryUrl
            })
            .Build();

        var services = new ServiceCollection();
        _ = services.AddOptions<UpdateSettings>()
                .Bind(configuration.GetSection("Updates"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        return services.BuildServiceProvider().GetRequiredService<IOptions<UpdateSettings>>();
    }

    [Fact]
    public void when_bound_then_github_repository_url_is_populated()
    {
        var options = BuildOptions("https://github.com/astar-development/astar-dev-mono");

        options.Value.GithubRepositoryUrl.ShouldBe("https://github.com/astar-development/astar-dev-mono");
    }
}
