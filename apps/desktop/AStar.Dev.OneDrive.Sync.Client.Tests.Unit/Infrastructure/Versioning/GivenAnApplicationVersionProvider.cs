using System.Reflection;
using System.Reflection.Emit;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Versioning;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Versioning;

public sealed class GivenAnApplicationVersionProvider
{
    [Fact]
    public void when_assembly_has_informational_version_then_current_version_is_that_value()
    {
        var assembly = typeof(GivenAnApplicationVersionProvider).Assembly;
        var expected = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;

        var provider = new ApplicationVersionProvider(assembly);

        provider.CurrentVersion.ShouldBe(expected);
    }

    [Fact]
    public void when_assembly_has_no_informational_version_then_current_version_falls_back_to_assembly_version()
    {
        var assemblyName = new AssemblyName("TestAssemblyWithoutInformationalVersion") { Version = new Version(1, 2, 3, 0) };
        var assemblyWithoutInformationalVersion = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

        var provider = new ApplicationVersionProvider(assemblyWithoutInformationalVersion);

        provider.CurrentVersion.ShouldBe("1.2.3.0");
    }
}
