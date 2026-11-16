namespace AStarDev.CloudSync.Classification.Sqlite;

internal sealed class CategoryEntity
{
    private string name = string.Empty;

    public Guid Id { get; set; }

    /// <summary>The name as given, kept for display and for reading back.</summary>
    public string Name
    {
        get => name;
        set
        {
            name = value;
            NameKey = value.ToUpperInvariant();
        }
    }

    /// <summary>The name folded to upper case with the invariant culture: the key that matches names ignoring case, non-ASCII letters included. It follows <see cref="Name"/> and is never set on its own.</summary>
    public string NameKey { get; set; } = string.Empty;
}
