using Fab4Kids.Web.Catalogue;

namespace Fab4Kids.Web.Homepage;

/// <summary>Factory for <see cref="FeaturedResource"/>.</summary>
public static class FeaturedResourceFactory
{
    public static FeaturedResource Create(PdfFile file, string subjectName, string subjectColor, string keyStageLabel, string href) =>
        new(file, subjectName, subjectColor, keyStageLabel, href);
}
