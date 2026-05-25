namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.ApplicationConfiguration;

/// <summary>Config-bound POCO for a single file classification rule, sourced from the <c>FileClassificationRules</c> section.</summary>
public sealed class FileClassificationRuleOptions
{
    /// <summary>One or more lowercase keywords to match against path tokens.</summary>
    public IReadOnlyList<string> Keywords { get; init; } = [];

    /// <summary>Top-level classification category (required).</summary>
    public string Level1 { get; init; } = string.Empty;

    /// <summary>Second-level classification category, or null if not used.</summary>
    public string? Level2 { get; init; }

    /// <summary>Third-level classification category, or null if not used.</summary>
    public string? Level3 { get; init; }

    /// <summary>Whether this classification should be flagged as special.</summary>
    public bool IsSpecial { get; init; }
}
