using System;
using System.IO;
using AStar.Dev.Guard.Clauses;
using Microsoft.Extensions.Configuration;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

/// <summary>
///     Resolves the Sqlite connection string used by <c>App.axaml.cs</c> to configure <c>AppDbContext</c>.
///     Reads the <c>ConnectionStrings:Sqlite</c> configuration key when present; otherwise falls back to the
///     default database path.
/// </summary>
public static class SqliteConnectionStringProvider
{
    /// <summary>
    ///     The connection string used when configuration does not supply <c>ScrapeConfiguration:ConnectionStrings:Sqlite</c>.
    /// </summary>
    public static readonly string DefaultConnectionString =
        $"Data Source={Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents", "Scrapper", "scrapper.db")}";

    /// <summary>
    ///     Resolves the Sqlite connection string from <paramref name="configuration" />, falling back to
    ///     <see cref="DefaultConnectionString" /> when not configured.
    /// </summary>
    public static string Get(IConfiguration configuration) => GuardAgainst.Null(configuration)["ScrapeConfiguration:ConnectionStrings:Sqlite"] ?? DefaultConnectionString;
}
