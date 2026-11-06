using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Extensions.Configuration;

namespace AStarDev.LoggingOTel.TestsUnit;

public sealed class GivenAnOTelResourceBuilder
{
    private static readonly IConfiguration EmptyConfiguration = new ConfigurationBuilder().Build();

    [Fact]
    public void when_the_service_name_is_blank_and_there_is_no_entry_assembly_then_the_service_name_is_unknown_service()
    {
        var attributes = BuildAttributes(EmptyConfiguration, " ", null, null);

        attributes["service.name"].ShouldBe("unknown_service");
    }

    [Fact]
    public void when_there_is_no_entry_assembly_then_no_service_version_is_set()
    {
        var attributes = BuildAttributes(EmptyConfiguration, "the-service", null, null);

        attributes.ShouldNotContainKey("service.version");
    }

    [Fact]
    public void when_the_entry_assembly_has_no_informational_version_then_no_service_version_is_set()
    {
        var assemblyWithoutVersion = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("the-unversioned-assembly"), AssemblyBuilderAccess.Run);

        var attributes = BuildAttributes(EmptyConfiguration, " ", assemblyWithoutVersion, null);

        attributes.ShouldNotContainKey("service.version");
        attributes["service.name"].ShouldBe("the-unversioned-assembly");
    }

    [Fact]
    public void when_no_deployment_environment_is_configured_then_the_dotnet_environment_is_used_in_lower_case()
    {
        var attributes = BuildAttributes(EmptyConfiguration, "the-service", null, "Development");

        attributes["deployment.environment.name"].ShouldBe("development");
    }

    [Fact]
    public void when_neither_a_deployment_environment_nor_a_dotnet_environment_is_set_then_production_is_used()
    {
        var attributes = BuildAttributes(EmptyConfiguration, "the-service", null, null);

        attributes["deployment.environment.name"].ShouldBe("production");
    }

    private static Dictionary<string, object> BuildAttributes(IConfiguration configuration, string serviceName, Assembly? entryAssembly, string? dotnetEnvironment)
        => OTelLoggingConfigurator.CreateResourceBuilder(configuration, serviceName, entryAssembly, dotnetEnvironment).Build().Attributes.ToDictionary(attribute => attribute.Key, attribute => attribute.Value);
}
