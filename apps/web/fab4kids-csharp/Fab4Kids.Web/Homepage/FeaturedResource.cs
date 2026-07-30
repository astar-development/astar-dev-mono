using Fab4Kids.Web.Catalogue;

namespace Fab4Kids.Web.Homepage;

/// <summary>A resource shown in the homepage "Fresh off the (virtual) press" grid, paired with its subject's brand accent.</summary>
public sealed record FeaturedResource(PdfFile File, string SubjectName, string SubjectColor, string KeyStageLabel, string Href);
