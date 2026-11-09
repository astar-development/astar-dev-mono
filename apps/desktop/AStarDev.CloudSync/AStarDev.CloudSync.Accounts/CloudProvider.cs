namespace AStarDev.CloudSync.Accounts;

/// <summary>The cloud storage provider an account belongs to.</summary>
public abstract record CloudProvider
{
    private CloudProvider()
    {
    }

    /// <summary>Microsoft OneDrive.</summary>
    public sealed record OneDrive : CloudProvider
    {
        internal OneDrive()
        {
        }
    }
}
