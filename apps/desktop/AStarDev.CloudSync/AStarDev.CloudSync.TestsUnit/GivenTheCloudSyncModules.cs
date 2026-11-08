using System.Reflection;

namespace AStarDev.CloudSync.TestsUnit;

public class GivenTheCloudSyncModules
{
    private const string CloudSyncPrefix = "AStarDev.CloudSync";

    private static readonly string[] GraphAndMsalPrefixes = ["Microsoft.Graph", "Microsoft.Identity.Client", "Microsoft.Kiota"];

    private static readonly string[] EfCorePrefixes = ["Microsoft.EntityFrameworkCore"];

    [Fact]
    public void when_the_host_is_loaded_then_every_module_is_part_of_it()
    {
        var modules = CloudSyncAssemblies().Select(assembly => assembly.GetName().Name);

        modules.ShouldBe(["AStarDev.CloudSync", "AStarDev.CloudSync.Accounts", "AStarDev.CloudSync.Accounts.OneDrive", "AStarDev.CloudSync.Accounts.Sqlite", "AStarDev.CloudSync.DisplaySettings", "AStarDev.CloudSync.Overview", "AStarDev.CloudSync.Sync", "AStarDev.CloudSync.Sync.OneDrive", "AStarDev.CloudSync.Sync.Sqlite", "AStarDev.CloudSync.ViewModels"], ignoreOrder: true);
    }

    [Fact]
    public void when_a_module_references_graph_or_msal_then_it_is_a_onedrive_module()
    {
        var modules = ModulesReferencing(GraphAndMsalPrefixes);

        modules.ShouldAllBe(name => name.EndsWith(".OneDrive", StringComparison.Ordinal));
    }

    [Fact]
    public void when_a_module_references_ef_core_then_it_is_a_sqlite_module()
    {
        var modules = ModulesReferencing(EfCorePrefixes);

        modules.ShouldAllBe(name => name.EndsWith(".Sqlite", StringComparison.Ordinal));
    }

    private static List<string> ModulesReferencing(string[] prefixes) =>
        [.. CloudSyncAssemblies()
            .Where(assembly => assembly.GetReferencedAssemblies().Any(reference => prefixes.Any(prefix => (reference.Name ?? string.Empty).StartsWith(prefix, StringComparison.Ordinal))))
            .Select(assembly => assembly.GetName().Name ?? string.Empty)];

    private static List<Assembly> CloudSyncAssemblies()
    {
        var host = typeof(ServiceCollectionExtensions).Assembly;
        var found = new Dictionary<string, Assembly>(StringComparer.Ordinal) { [host.GetName().Name ?? string.Empty] = host };
        var pending = new Queue<Assembly>([host]);

        while (pending.Count > 0)
        {
            foreach (var reference in pending.Dequeue().GetReferencedAssemblies().Where(reference => IsCloudSync(reference) && !found.ContainsKey(reference.Name ?? string.Empty)))
            {
                var loaded = Assembly.Load(reference);
                found[reference.Name ?? string.Empty] = loaded;
                pending.Enqueue(loaded);
            }
        }

        return [.. found.Values];
    }

    private static bool IsCloudSync(AssemblyName reference) => reference.Name is { } name && (name == CloudSyncPrefix || name.StartsWith(CloudSyncPrefix + ".", StringComparison.Ordinal));
}
