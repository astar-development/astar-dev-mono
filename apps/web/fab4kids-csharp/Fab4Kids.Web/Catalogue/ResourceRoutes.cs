namespace Fab4Kids.Web.Catalogue;

/// <summary>Builds the URL for a resource's detail page, shared by every page that links into it.</summary>
public static class ResourceRoutes
{
    public static string DetailHref(string subjectSlug, int fileId) => $"/{subjectSlug}/resource/{fileId}";
}
