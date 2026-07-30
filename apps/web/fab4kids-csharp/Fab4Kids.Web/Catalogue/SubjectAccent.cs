namespace Fab4Kids.Web.Catalogue;

/// <summary>Brand metadata for a subject (colour, initial letter, blurb, listing page link), shared by the homepage subject grid, subject hero, and resource cards.</summary>
public sealed record SubjectAccent(string Name, string Letter, string Color, string Description, string Href);
