namespace AStarDev.CloudSync.Sync;

/// <summary>Why a provider will not accept an item's name, path or size. The item is skipped and reported, never renamed.</summary>
public abstract record InvalidRemoteName
{
    private InvalidRemoteName()
    {
    }

    /// <summary>The name has a character the provider rejects (quote, asterisk, colon, angle brackets, question mark, slashes or pipe).</summary>
    public sealed record IllegalCharacter : InvalidRemoteName
    {
        internal IllegalCharacter()
        {
        }
    }

    /// <summary>The name starts or ends with a space.</summary>
    public sealed record LeadingOrTrailingSpace : InvalidRemoteName
    {
        internal LeadingOrTrailingSpace()
        {
        }
    }

    /// <summary>The name ends with a dot.</summary>
    public sealed record EndsWithDot : InvalidRemoteName
    {
        internal EndsWithDot()
        {
        }
    }

    /// <summary>The name is reserved (CON, PRN, AUX, NUL, COM0-9, LPT0-9, .lock, desktop.ini, ~$ prefix or _vti_).</summary>
    public sealed record ReservedName : InvalidRemoteName
    {
        internal ReservedName()
        {
        }
    }

    /// <summary>The path is longer than the provider allows.</summary>
    public sealed record PathTooLong : InvalidRemoteName
    {
        internal PathTooLong()
        {
        }
    }

    /// <summary>The file is larger than the provider allows.</summary>
    public sealed record FileTooLarge : InvalidRemoteName
    {
        internal FileTooLarge()
        {
        }
    }
}
