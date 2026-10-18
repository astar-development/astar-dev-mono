namespace AStarDev.EFCoreExtensions;

/// <summary>
/// The database provider the model is being configured for. Controls provider-specific key generation.
/// </summary>
public enum DatabaseProvider
{
    /// <summary>
    /// SQLite. Generated integer keys use <c>AUTOINCREMENT</c>.
    /// </summary>
    Sqlite,

    /// <summary>
    /// SQL Server. Relies on the provider's default identity strategy.
    /// </summary>
    SqlServer,

    /// <summary>
    /// PostgreSQL. Relies on the provider's default identity strategy.
    /// </summary>
    PostgreSql
}
