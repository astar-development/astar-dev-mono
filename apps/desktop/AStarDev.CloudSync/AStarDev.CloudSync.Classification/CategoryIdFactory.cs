namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="CategoryId"/> values.</summary>
public static class CategoryIdFactory
{
    /// <summary>Creates the id that wraps <paramref name="value"/>. The caller supplies the value, so this factory stays deterministic.</summary>
    /// <param name="value">The unique value, for example from <see cref="Guid.NewGuid"/>.</param>
    public static CategoryId Create(Guid value) => new(value);
}
