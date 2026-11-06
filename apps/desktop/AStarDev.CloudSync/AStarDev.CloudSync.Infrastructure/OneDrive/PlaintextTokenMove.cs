namespace AStarDev.CloudSync.Infrastructure.OneDrive;

/// <summary>What happened to an old plaintext token file once the secure store worked again.</summary>
public abstract record PlaintextTokenMove
{
    private PlaintextTokenMove()
    {
    }

    /// <summary>The tokens were moved into the secure store and the plaintext file was deleted.</summary>
    public sealed record Moved : PlaintextTokenMove;

    /// <summary>There was no plaintext file.</summary>
    public sealed record NothingToMove : PlaintextTokenMove;
}
