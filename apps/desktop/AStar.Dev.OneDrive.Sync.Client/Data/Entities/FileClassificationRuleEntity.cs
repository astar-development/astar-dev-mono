namespace AStar.Dev.OneDrive.Sync.Client.Data.Entities;

/// <summary>Persisted file classification rule, matched against path tokens during sync.</summary>
public sealed class FileClassificationRuleEntity
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Pipe-delimited keywords matched against path tokens (e.g. "photos|photo|img").</summary>
    public string Keywords { get; set; } = string.Empty;

    /// <summary>Top-level classification category.</summary>
    public string Level1 { get; set; } = string.Empty;

    /// <summary>Second-level classification category, or null if not applicable.</summary>
    public string? Level2 { get; set; }

    /// <summary>Third-level semantic subcategory, or null if not applicable.</summary>
    public string? Level3 { get; set; }

    /// <summary>Whether this classification is flagged as special.</summary>
    public bool IsSpecial { get; set; }
}
