namespace AStarDev.CloudSync.Sync;

/// <summary>What happened in an activity entry.</summary>
public abstract record ActivityKind
{
    private ActivityKind()
    {
    }

    /// <summary>Scanning the local folders for changes.</summary>
    public sealed record DetectingChanges : ActivityKind
    {
        internal DetectingChanges()
        {
        }
    }

    /// <summary>Files were uploaded.</summary>
    public sealed record Uploaded : ActivityKind
    {
        internal Uploaded(int fileCount, long bytes)
        {
            FileCount = fileCount;
            Bytes = bytes;
        }

        /// <summary>The number of files uploaded.</summary>
        public int FileCount { get; }

        /// <summary>The number of bytes uploaded.</summary>
        public long Bytes { get; }
    }

    /// <summary>Files were downloaded.</summary>
    public sealed record Downloaded : ActivityKind
    {
        internal Downloaded(int fileCount)
        {
            FileCount = fileCount;
        }

        /// <summary>The number of files downloaded.</summary>
        public int FileCount { get; }
    }

    /// <summary>A file changed on both sides.</summary>
    public sealed record ConflictDetected : ActivityKind
    {
        internal ConflictDetected(string fileName)
        {
            FileName = fileName;
        }

        /// <summary>The name of the file in conflict.</summary>
        public string FileName { get; }
    }

    /// <summary>The account signed in.</summary>
    public sealed record Authenticated : ActivityKind
    {
        internal Authenticated()
        {
        }
    }
}
