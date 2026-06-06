using AStar.Dev.OneDrive.Sync.Client.Infrastructure.ApplicationConfiguration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.ApplicationConfiguration;

public sealed class GivenClientConfiguration
{
    private static IOptions<ClientConfiguration> BuildOptions(string applicationName, string applicationVersion)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AStarDevOneDriveClient:ApplicationName"] = applicationName,
                ["AStarDevOneDriveClient:ApplicationVersion"] = applicationVersion
            })
            .Build();

        var services = new ServiceCollection();
        _ = services.AddOptions<ClientConfiguration>()
                .Bind(configuration.GetSection("AStarDevOneDriveClient"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        return services.BuildServiceProvider().GetRequiredService<IOptions<ClientConfiguration>>();
    }

    [Fact]
    public void when_bound_then_application_name_is_populated()
    {
        var options = BuildOptions("AStar Dev OneDrive Sync Client", "0.3.0");

        options.Value.ApplicationName.ShouldBe("AStar Dev OneDrive Sync Client");
    }

    [Fact]
    public void when_bound_then_application_version_is_populated()
    {
        var options = BuildOptions("AStar Dev OneDrive Sync Client", "0.3.0");

        options.Value.ApplicationVersion.ShouldBe("0.3.0");
    }
}
