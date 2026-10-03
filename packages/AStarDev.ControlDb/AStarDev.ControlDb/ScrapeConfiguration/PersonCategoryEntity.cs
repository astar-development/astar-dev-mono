namespace AStarDev.ControlDb.ScrapeConfiguration;

/// <summary>A Wallhaven tag category whose tags are person names, used to prefix wallpaper file names.</summary>
public sealed class PersonCategoryEntity : AuditableEntity
{
    /// <summary>The default person categories applied to a new search configuration.</summary>
    public static readonly IReadOnlyList<string> DefaultNames = ["Celebrities", "Models", "Pornstars", "Other Figures", "Actress"];

    /// <summary>The unique identifier for the person category.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Foreign key to the parent search configuration.</summary>
    public SearchConfigurationId SearchConfigurationId { get; set; }

    /// <summary>Navigation property to the parent search configuration.</summary>
    public SearchConfigurationEntity? SearchConfiguration { get; set; }

    /// <summary>The Wallhaven tag category name, matched case-insensitively.</summary>
    public string Name { get; set; } = string.Empty;
}
