namespace AStarDev.CloudSync.Classification;

/// <summary>A word or phrase that classifies files into a category. The text is trimmed, composed, lower case and single spaced.</summary>
public sealed record ClassificationTerm
{
    internal ClassificationTerm(TermId id, string text, CategoryId categoryId, TermSource source, TermStatus status)
    {
        Id = id;
        Text = text;
        CategoryId = categoryId;
        Source = source;
        Status = status;
    }

    /// <summary>Returns a redacted form, because the text can come from a user's file names.</summary>
    public override string ToString() => "ClassificationTerm(redacted)";

    /// <summary>The id.</summary>
    public TermId Id { get; }

    /// <summary>The normalised text.</summary>
    public string Text { get; }

    /// <summary>The category the term belongs to.</summary>
    public CategoryId CategoryId { get; }

    /// <summary>Where the term came from.</summary>
    public TermSource Source { get; }

    /// <summary>Whether the term is active or ignored.</summary>
    public TermStatus Status { get; }
}
