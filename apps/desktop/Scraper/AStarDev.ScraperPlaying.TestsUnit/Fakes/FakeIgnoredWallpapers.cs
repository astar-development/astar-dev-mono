using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.TestsUnit.Fakes;

/// <summary>An ignored-wallpaper store whose results are set by the test and which records the handles it was asked to remember and how often it was asked to forget them.</summary>
internal sealed class FakeIgnoredWallpapers : IIgnoredWallpapers
{
    /// <summary>The handles passed to <see cref="Record"/>, in order.</summary>
    public List<FileHandle> Recorded { get; } = [];

    /// <summary>The result of <see cref="Record"/>.</summary>
    public Exceptional<Unit> RecordResult { get; set; } = Unit.Instance;

    /// <summary>The number of times <see cref="ForgetAllAsync"/> was called.</summary>
    public int ForgetCount { get; private set; }

    /// <summary>The result of <see cref="ForgetAllAsync"/>.</summary>
    public Exceptional<int> ForgetResult { get; set; } = 0;

    public Exceptional<Unit> Record(FileHandle fileHandle)
    {
        Recorded.Add(fileHandle);

        return RecordResult;
    }

    public Task<Exceptional<int>> ForgetAllAsync(CancellationToken cancellationToken = default)
    {
        ForgetCount++;

        return Task.FromResult(ForgetResult);
    }
}
