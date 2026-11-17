using AStarDev.CloudSync.Classification;

namespace AStarDev.CloudSync.ViewModels.Classification;

/// <summary>Where the reclassify button is: waiting, running, or how the last run ended.</summary>
public abstract record ReclassifyState
{
    private ReclassifyState()
    {
    }

    /// <summary>No run has started since the application opened.</summary>
    public sealed record Idle : ReclassifyState
    {
        internal Idle()
        {
        }
    }

    /// <summary>A run is in progress.</summary>
    public sealed record Running : ReclassifyState
    {
        internal Running(int filesDone, int filesSkipped)
        {
            FilesDone = filesDone;
            FilesSkipped = filesSkipped;
        }

        /// <summary>The files written so far.</summary>
        public int FilesDone { get; }

        /// <summary>The files skipped so far because their account was busy or removed.</summary>
        public int FilesSkipped { get; }
    }

    /// <summary>A run this page did not start is in progress, such as the startup reclassify. The page cannot cancel it.</summary>
    public sealed record Observing : ReclassifyState
    {
        internal Observing(int filesDone, int filesSkipped)
        {
            FilesDone = filesDone;
            FilesSkipped = filesSkipped;
        }

        /// <summary>The files written so far.</summary>
        public int FilesDone { get; }

        /// <summary>The files skipped so far because their account was busy or removed.</summary>
        public int FilesSkipped { get; }
    }

    /// <summary>The last run wrote every file.</summary>
    public sealed record Completed : ReclassifyState
    {
        internal Completed(ReclassifyCounts counts) => Counts = counts;

        /// <summary>What the run did.</summary>
        public ReclassifyCounts Counts { get; }
    }

    /// <summary>The user cancelled the last run.</summary>
    public sealed record Cancelled : ReclassifyState
    {
        internal Cancelled(ReclassifyCounts counts) => Counts = counts;

        /// <summary>What the run did before the cancel.</summary>
        public ReclassifyCounts Counts { get; }
    }

    /// <summary>The last run failed part way.</summary>
    public sealed record Failed : ReclassifyState
    {
        internal Failed(ReclassifyCounts counts) => Counts = counts;

        /// <summary>What the run did before the failure.</summary>
        public ReclassifyCounts Counts { get; }
    }

    /// <summary>Another run was already in progress, so nothing started.</summary>
    public sealed record AlreadyRunning : ReclassifyState
    {
        internal AlreadyRunning()
        {
        }
    }
}
