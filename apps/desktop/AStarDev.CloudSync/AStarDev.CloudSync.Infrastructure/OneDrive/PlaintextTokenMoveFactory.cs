namespace AStarDev.CloudSync.Infrastructure.OneDrive;

/// <summary>Creates <see cref="PlaintextTokenMove"/> cases.</summary>
public static class PlaintextTokenMoveFactory
{
    /// <summary>Creates the case for tokens moved into the secure store.</summary>
    public static PlaintextTokenMove CreateMoved() => new PlaintextTokenMove.Moved();

    /// <summary>Creates the case for no plaintext file.</summary>
    public static PlaintextTokenMove CreateNothingToMove() => new PlaintextTokenMove.NothingToMove();
}
