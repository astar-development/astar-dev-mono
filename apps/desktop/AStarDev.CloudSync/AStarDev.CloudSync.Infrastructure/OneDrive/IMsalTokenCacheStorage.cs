namespace AStarDev.CloudSync.Infrastructure.OneDrive;

/// <summary>Connects the MSAL token cache to persistent storage.</summary>
public interface IMsalTokenCacheStorage
{
    /// <summary>Connects the token cache to the operating system's secure store, such as the Linux keyring. Throws if the store is unavailable.</summary>
    Task AttachSecureStoreAsync();

    /// <summary>Connects the token cache to a plaintext file readable only by the current user. Throws if the file cannot be used.</summary>
    Task AttachPlaintextFileAsync();

    /// <summary>Moves the tokens of an old plaintext token file into the secure store, then deletes the file. Call it only after <see cref="AttachSecureStoreAsync"/> succeeded. Throws if the move fails; the file is then kept.</summary>
    /// <returns><see cref="PlaintextTokenMove.Moved"/>, or <see cref="PlaintextTokenMove.NothingToMove"/> when there is no plaintext file.</returns>
    Task<PlaintextTokenMove> MovePlaintextFileToSecureStoreAsync();

    /// <summary>Stops the token cache using the plaintext file if it does, forgets the accounts it held in memory, and deletes the file. Throws if the file cannot be deleted.</summary>
    Task DetachPlaintextFileAsync();
}
