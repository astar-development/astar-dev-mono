using System.IO.Abstractions;

namespace AStarDev.SingleInstance;

/// <summary>Where the lock and hand-off socket live and how long a second launch waits for an answer.</summary>
public sealed record SingleInstanceOptions
{
    internal SingleInstanceOptions(IDirectoryInfo dataFolder, string name, TimeSpan handOffTimeout)
    {
        DataFolder = dataFolder;
        Name = name;
        HandOffTimeout = handOffTimeout;
    }

    /// <summary>The per-user folder that holds <c>{Name}.lock</c> and <c>{Name}.sock</c>.</summary>
    public IDirectoryInfo DataFolder { get; }

    /// <summary>The file name stem for the lock and the socket.</summary>
    public string Name { get; }

    /// <summary>How long a second launch waits for the running instance to answer.</summary>
    public TimeSpan HandOffTimeout { get; }

    /// <summary>Returns a redacted form, so the folder path never reaches logs by accident.</summary>
    public override string ToString() => "SingleInstanceOptions(redacted)";
}
