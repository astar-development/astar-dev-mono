using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AStar.Dev.Velopack.Publishing.Tests.Unit;

public sealed class GivenVelopackUpdateSettings
{
    private static IOptions<VelopackUpdateSettings> BuildOptions(string githubRepositoryUrl, string channelPrefix = "clock")
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Updates:GithubRepositoryUrl"] = githubRepositoryUrl,
                ["Updates:ChannelPrefix"]       = channelPrefix
            })
            .Build();

        var services = new ServiceCollection();
        _ = services.AddOptions<VelopackUpdateSettings>()
                .Bind(configuration.GetSection(VelopackUpdateSettings.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        return services.BuildServiceProvider().GetRequiredService<IOptions<VelopackUpdateSettings>>();
    }

    [Fact]
    public void when_bound_then_github_repository_url_is_populated()
    {
        var options = BuildOptions("https://github.com/astar-development/astar-dev-mono");

        options.Value.GithubRepositoryUrl.ShouldBe(new Uri("https://github.com/astar-development/astar-dev-mono"));
    }

    [Fact]
    public void when_bound_then_channel_prefix_is_populated()
    {
        var options = BuildOptions("https://github.com/astar-development/astar-dev-mono", "onedrive-sync");

        options.Value.ChannelPrefix.ShouldBe("onedrive-sync");
    }

    [Fact]
    public void when_section_name_requested_then_it_is_updates()
        => VelopackUpdateSettings.SectionName.ShouldBe("Updates");
}
