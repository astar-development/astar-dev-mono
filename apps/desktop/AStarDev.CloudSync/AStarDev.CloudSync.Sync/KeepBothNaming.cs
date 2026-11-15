namespace AStarDev.CloudSync.Sync;

/// <summary>Names a Keep Both copy as of now.</summary>
/// <param name="namer">Builds the name.</param>
/// <param name="time">Stamps the name.</param>
internal sealed class KeepBothNaming(IKeepBothNamer namer, TimeProvider time)
{
    internal string NameNow(string original, IReadOnlyCollection<string> existingNames) => namer.Name(original, time.GetUtcNow(), existingNames);
}
