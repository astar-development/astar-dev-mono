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

    /// <summary>BCP-47 locale code (e.g. <c>en-GB</c>). Defaults to <c>en-GB</c> (the only MVP locale).</summary>
    public string Locale { get; set; } = "en-GB";

    /// <summary>User type preference: Casual or PowerUser (OH-04, AM-05).</summary>
    public string UserType { get; set; } = "Casual";
}
