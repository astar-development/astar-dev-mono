using System.Reflection;

namespace AStarDev.CloudSync.TestsUnit;

public sealed class GivenTheCloudSyncArchitecture
{
    private static readonly string[] CoreAssemblies = ["AStarDev.CloudSync.Accounts", "AStarDev.CloudSync.Sync", "AStarDev.CloudSync.Overview", "AStarDev.CloudSync.DisplaySettings", "AStarDev.CloudSync.ViewModels"];

    private static readonly string[] ForbiddenInCore = ["Avalonia", "Microsoft.Graph", "Microsoft.Kiota", "Microsoft.Identity.Client", "Microsoft.EntityFrameworkCore"];

    private static readonly string[] Infrastructure = ["AStarDev.CloudSync.Accounts.OneDrive", "AStarDev.CloudSync.Accounts.Sqlite", "AStarDev.CloudSync.Sync.OneDrive", "AStarDev.CloudSync.Sync.Sqlite"];

    public static TheoryData<string> Core => [.. CoreAssemblies];

    private static IEnumerable<string> ReferencesOf(string assemblyName) => Assembly.Load(assemblyName).GetReferencedAssemblies().Select(reference => reference.Name ?? string.Empty);

    private static DirectoryInfo CloudSyncFolder() =>
        new(typeof(GivenTheCloudSyncArchitecture).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(attribute => attribute.Key == "CloudSyncSourceFolder").Value ?? throw new InvalidOperationException("The build records where the CloudSync projects are."));

    [Theory]
    [MemberData(nameof(Core))]
    public void when_a_core_project_is_read_then_it_references_no_graph_msal_ef_or_avalonia_assembly(string assemblyName)
    {
        var offenders = ReferencesOf(assemblyName).Where(reference => ForbiddenInCore.Any(forbidden => reference.StartsWith(forbidden, StringComparison.Ordinal))).Distinct().ToList();

        offenders.ShouldBeEmpty($"{assemblyName} must stay free of provider, database and UI toolkit assemblies so the engine can be tested alone.");
    }

    [Theory]
    [MemberData(nameof(Core))]
    public void when_a_core_project_is_read_then_it_does_not_reference_an_infrastructure_project(string assemblyName)
    {
        ReferencesOf(assemblyName).Where(reference => Infrastructure.Contains(reference)).ShouldBeEmpty();
    }

    [Fact]
    public void when_the_engine_is_read_then_it_does_not_reference_the_view_models()
    {
        ReferencesOf("AStarDev.CloudSync.Sync").ShouldNotContain("AStarDev.CloudSync.ViewModels");
    }

    [Fact]
    public void when_a_production_project_exists_then_it_has_a_unit_or_integration_test_project()
    {
        var root = CloudSyncFolder();
        var projects = root.GetDirectories("AStarDev.CloudSync*").Where(folder => !folder.Name.Contains("Test", StringComparison.Ordinal) && File.Exists(Path.Combine(folder.FullName, folder.Name + ".csproj"))).Select(folder => folder.Name);

        var withoutTests = projects.Where(project => !Directory.Exists(Path.Combine(root.FullName, project + ".TestsUnit")) && !Directory.Exists(Path.Combine(root.FullName, project + ".TestsIntegration"))).ToList();

        withoutTests.ShouldBeEmpty("Every production project needs a *.TestsUnit or *.TestsIntegration project beside it.");
    }
}
