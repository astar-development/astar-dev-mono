namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="InvalidRemoteName"/> cases.</summary>
public static class InvalidRemoteNameFactory
{
    /// <summary>Creates the <see cref="InvalidRemoteName.IllegalCharacter"/> case.</summary>
    public static InvalidRemoteName CreateIllegalCharacter() => new InvalidRemoteName.IllegalCharacter();

    /// <summary>Creates the <see cref="InvalidRemoteName.LeadingOrTrailingSpace"/> case.</summary>
    public static InvalidRemoteName CreateLeadingOrTrailingSpace() => new InvalidRemoteName.LeadingOrTrailingSpace();

    /// <summary>Creates the <see cref="InvalidRemoteName.EndsWithDot"/> case.</summary>
    public static InvalidRemoteName CreateEndsWithDot() => new InvalidRemoteName.EndsWithDot();

    /// <summary>Creates the <see cref="InvalidRemoteName.ReservedName"/> case.</summary>
    public static InvalidRemoteName CreateReservedName() => new InvalidRemoteName.ReservedName();

    /// <summary>Creates the <see cref="InvalidRemoteName.PathTooLong"/> case.</summary>
    public static InvalidRemoteName CreatePathTooLong() => new InvalidRemoteName.PathTooLong();

    /// <summary>Creates the <see cref="InvalidRemoteName.FileTooLarge"/> case.</summary>
    public static InvalidRemoteName CreateFileTooLarge() => new InvalidRemoteName.FileTooLarge();
}
