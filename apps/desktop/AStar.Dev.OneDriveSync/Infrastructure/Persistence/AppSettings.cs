namespace AStar.Dev.OneDriveSync.Infrastructure.Persistence;

/// <summary>
///     Single-row application settings record stored in SQLite (AC TH-02).
///     Always accessed via ID <see cref="SingletonId" /> — there is exactly one row.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Fixed primary key value — the table always holds exactly one row.</summary>
    public const int SingletonId = 1;

    /// <summary>EF Core primary key.</summary>
    public int Id { get; set; } = SingletonId;

    /// <summary>Stored theme mode string (matches <see cref="Infrastructure.Theming.ThemeMode" /> enum names).</summary>
    public string ThemeMode { get; set; } = nameof(Theming.ThemeMode.Auto);
}
