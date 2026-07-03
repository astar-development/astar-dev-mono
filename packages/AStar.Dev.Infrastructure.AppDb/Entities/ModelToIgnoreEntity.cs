namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>A model whose files should be ignored completely by the scraper.</summary>
public sealed class ModelToIgnoreEntity : AuditableEntity
{
    /// <summary>Primary key.</summary>
    public ModelId Id { get; set; } = new(Guid.CreateVersion7());

    /// <summary>The value of the model to ignore completely.</summary>
    public string Value { get; set; } = string.Empty;
}
