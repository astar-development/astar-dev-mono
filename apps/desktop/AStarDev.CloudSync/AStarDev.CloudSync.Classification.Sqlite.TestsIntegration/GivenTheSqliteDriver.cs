using System.Reflection;
using Microsoft.Data.Sqlite;

namespace AStarDev.CloudSync.Classification.Sqlite.TestsIntegration;

public sealed class GivenTheSqliteDriver
{
    private static readonly Version FirstVersionWithTheConnectionPoolFix = new(10, 0, 13);

    [Fact]
    public void when_the_driver_is_loaded_then_it_has_the_connection_pool_fix_for_parallel_opens()
    {
        var informationalVersion = typeof(SqliteConnection).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";

        var loadedVersion = Version.Parse(informationalVersion.Split('+', '-')[0]);

        loadedVersion.ShouldBeGreaterThanOrEqualTo(FirstVersionWithTheConnectionPoolFix, $"Microsoft.Data.Sqlite.Core {informationalVersion} can lend one pooled connection to two parallel openers (dotnet/efcore#39008); pin 10.0.13 or later in Directory.Packages.props");
    }
}
