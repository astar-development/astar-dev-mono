namespace AStarDev.CloudSync.Infrastructure.OneDrive;

/// <summary>Connects the MSAL token cache to persistent storage.</summary>
public interface IMsalTokenCacheStorage
{
    /// <summary>Connects the token cache to the operating system's secure store, such as the Linux keyring. Throws if the store is unavailable.</summary>
    Task AttachSecureStoreAsync();

    /// <summary>Connects the token cache to a plaintext file readable only by the current user. Throws if the file cannot be used.</summary>
    Task AttachPlaintextFileAsync();
}
