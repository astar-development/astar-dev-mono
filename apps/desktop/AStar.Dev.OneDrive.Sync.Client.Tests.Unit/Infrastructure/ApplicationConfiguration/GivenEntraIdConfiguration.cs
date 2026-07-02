using AStar.Dev.OneDrive.Sync.Client.Infrastructure.ApplicationConfiguration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.ApplicationConfiguration;

public sealed class GivenEntraIdConfiguration
{
    private static IOptions<EntraIdConfiguration> BuildOptions(string clientId, string redirectUri, string authority, IEnumerable<string> scopes)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EntraId:ClientId"] = clientId,
                ["EntraId:RedirectUri"] = redirectUri,
                ["EntraId:AuthorityForMicrosoftAccountsOnly"] = authority,
                ["EntraId:Scopes:0"] = scopes.ElementAt(0),
                ["EntraId:Scopes:1"] = scopes.ElementAt(1)
            })
            .Build();

        var services = new ServiceCollection();
        _ = services.AddOptions<EntraIdConfiguration>()
                .Bind(configuration.GetSection("EntraId"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        return services.BuildServiceProvider().GetRequiredService<IOptions<EntraIdConfiguration>>();
    }

    [Fact]
    public void when_bound_then_client_id_is_populated()
    {
        var options = BuildOptions("test-client-id", "http://localhost", "https://login.microsoftonline.com/consumers", ["User.Read", "Files.ReadWrite.All"]);

        options.Value.ClientId.ShouldBe("test-client-id");
    }

    [Fact]
    public void when_bound_then_redirect_uri_is_populated()
    {
        var options = BuildOptions("test-client-id", "http://localhost", "https://login.microsoftonline.com/consumers", ["User.Read", "Files.ReadWrite.All"]);

        options.Value.RedirectUri.ShouldBe("http://localhost");
    }

    [Fact]
    public void when_bound_then_authority_is_populated()
    {
        var options = BuildOptions("test-client-id", "http://localhost", "https://login.microsoftonline.com/consumers", ["User.Read", "Files.ReadWrite.All"]);

        options.Value.AuthorityForMicrosoftAccountsOnly.ShouldBe("https://login.microsoftonline.com/consumers");
    }

    [Fact]
    public void when_bound_then_scopes_are_populated()
    {
        var options = BuildOptions("test-client-id", "http://localhost", "https://login.microsoftonline.com/consumers", ["User.Read", "Files.ReadWrite.All"]);

        options.Value.Scopes.ShouldBe(["User.Read", "Files.ReadWrite.All"]);
    }
}
