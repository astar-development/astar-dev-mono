namespace AStarDev.CloudSync.ViewModels.Layouts;

/// <summary>The page layout A shows beside its account list.</summary>
public abstract record LayoutAPage
{
    private LayoutAPage()
    {
    }

    /// <summary>The account cards and stats.</summary>
    public sealed record Accounts : LayoutAPage
    {
        internal Accounts()
        {
        }
    }

    /// <summary>The folder selection of the selected account.</summary>
    public sealed record Folders : LayoutAPage
    {
        internal Folders()
        {
        }
    }

    /// <summary>The conflicts of every account.</summary>
    public sealed record Conflicts : LayoutAPage
    {
        internal Conflicts()
        {
        }
    }
}
