namespace AStarDev.CloudSync.Classification;

/// <summary>A term in the shipped seed data.</summary>
public sealed record SeedTerm
{
    internal SeedTerm(string text, string categoryName, SeedTermKind kind)
    {
        Text = text;
        CategoryName = categoryName;
        Kind = kind;
    }

    /// <summary>Returns a redacted form, because the text can come from a user's file names.</summary>
    public override string ToString() => "SeedTerm(redacted)";

    /// <summary>The normalised text.</summary>
    public string Text { get; }

    /// <summary>The name of the term's category.</summary>
    public string CategoryName { get; }

    /// <summary>What the term matches.</summary>
    public SeedTermKind Kind { get; }
}
