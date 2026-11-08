namespace AStarDev.CloudSync.Accounts.OneDrive;

/// <summary>Whether connecting the token cache may ask the user for plaintext-file consent.</summary>
internal abstract record ConsentPrompt
{
    private ConsentPrompt()
    {
    }

    /// <summary>The user started this, so the consent question may be shown.</summary>
    internal sealed record Allowed : ConsentPrompt;

    /// <summary>A background caller: the question is never shown and a missing consent fails the attempt.</summary>
    internal sealed record Never : ConsentPrompt;
}
