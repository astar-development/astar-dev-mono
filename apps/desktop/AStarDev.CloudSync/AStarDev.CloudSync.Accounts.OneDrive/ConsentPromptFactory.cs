namespace AStarDev.CloudSync.Accounts.OneDrive;

/// <summary>Creates <see cref="ConsentPrompt"/> cases.</summary>
internal static class ConsentPromptFactory
{
    /// <summary>Creates the case for a user-started caller.</summary>
    public static ConsentPrompt CreateAllowed() => new ConsentPrompt.Allowed();

    /// <summary>Creates the case for a background caller.</summary>
    public static ConsentPrompt CreateNever() => new ConsentPrompt.Never();
}
