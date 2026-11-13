namespace AStarDev.CloudSync.Sync;

/// <summary>Why a local file operation of the download pass failed (#1400).</summary>
public abstract record LocalOperationFailure
{
    private LocalOperationFailure()
    {
    }

    /// <summary>The local item changed after the plan was made, so it was left alone.</summary>
    public sealed record TargetChanged : LocalOperationFailure
    {
        internal TargetChanged()
        {
        }
    }

    /// <summary>The operating system denied access.</summary>
    public sealed record AccessDenied : LocalOperationFailure
    {
        internal AccessDenied()
        {
        }
    }

    /// <summary>Another program holds the file.</summary>
    public sealed record Locked : LocalOperationFailure
    {
        internal Locked()
        {
        }
    }

    /// <summary>The volume has too little free space.</summary>
    public sealed record InsufficientDiskSpace : LocalOperationFailure
    {
        internal InsufficientDiskSpace()
        {
        }
    }

    /// <summary>The path is longer than the file system allows.</summary>
    public sealed record PathTooLong : LocalOperationFailure
    {
        internal PathTooLong()
        {
        }
    }

    /// <summary>Anything else.</summary>
    public sealed record Unexpected : LocalOperationFailure
    {
        internal Unexpected()
        {
        }
    }
}
