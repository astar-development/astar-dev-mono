using System.Reflection;
using AStarDev.OneDriveSyncClient.Infrastructure.Versioning;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Versioning;

public sealed class GivenAnApplicationVersionProvider
{
    [Fact]
    public void when_the_sync_client_assembly_has_an_informational_version_then_current_version_is_that_value()
    {
        string expected = typeof(ApplicationVersionProvider).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;

        var provider = new ApplicationVersionProvider();

        provider.CurrentVersion.ShouldBe(expected);
    }
}
