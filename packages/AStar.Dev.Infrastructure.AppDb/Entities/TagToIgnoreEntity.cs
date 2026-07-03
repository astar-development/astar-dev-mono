namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>A tag that should cause a scraped file to be ignored.</summary>
public sealed class TagToIgnoreEntity : AuditableEntity
{
    /// <summary>Primary key.</summary>
    public TagId Id { get; set; } = new(Guid.CreateVersion7());

    /// <summary>The value of the tag to ignore.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Whether the image should be ignored irrespective of any other setting.</summary>
    public bool IgnoreImage { get; set; }
}
