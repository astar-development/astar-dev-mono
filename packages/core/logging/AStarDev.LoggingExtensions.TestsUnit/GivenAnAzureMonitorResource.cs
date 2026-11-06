using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using OpenTelemetry.Resources;

namespace AStarDev.LoggingExtensions.TestsUnit;

public sealed class GivenAnAzureMonitorResource
{
    private static readonly HostingEnvironment Environment = new() { ApplicationName = "the-host-application", EnvironmentName = Environments.Development };

    [Fact]
    public void when_the_service_name_is_blank_then_the_host_application_name_is_used()
    {
        var attributes = BuildAttributes(" ", null);

        attributes["service.name"].ShouldBe("the-host-application");
    }

    [Fact]
    public void when_there_is_no_entry_assembly_then_no_service_version_is_set()
    {
        var attributes = BuildAttributes("the-service", null);

        attributes.ShouldNotContainKey("service.version");
    }

    [Fact]
    public void when_the_entry_assembly_has_no_informational_version_then_no_service_version_is_set()
    {
        var assemblyWithoutVersion = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("the-unversioned-assembly"), AssemblyBuilderAccess.Run);

        var attributes = BuildAttributes("the-service", assemblyWithoutVersion);

        attributes.ShouldNotContainKey("service.version");
    }

    private static Dictionary<string, object> BuildAttributes(string serviceName, Assembly? entryAssembly)
    {
        var resource = ResourceBuilder.CreateEmpty();
        LoggingExtensions.ConfigureResource(resource, Environment, serviceName, entryAssembly);

        return resource.Build().Attributes.ToDictionary(attribute => attribute.Key, attribute => attribute.Value);
    }
}
