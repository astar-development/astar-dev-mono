namespace AStarDev.CloudSync.Sync;

/// <summary>Names the local copy a Keep Both resolution keeps (CR-04, #1406). Pure.</summary>
public interface IKeepBothNamer
{
    /// <summary>Builds <c>original-name-(yyyy-MM-ddTHHmmssZ).ext</c> in UTC with the invariant culture. When that name is taken, a counter is added: <c>original-name-(yyyy-MM-ddTHHmmssZ)-2.ext</c>, then <c>-3</c> and so on. Names are compared ignoring case.</summary>
    /// <param name="original">The file name, with its extension and without a folder.</param>
    /// <param name="utc">The moment to stamp; any offset is converted to UTC.</param>
    /// <param name="existingNames">The names already in the folder.</param>
    /// <returns>A name that is not in <paramref name="existingNames"/>.</returns>
    string Name(string original, DateTimeOffset utc, IReadOnlyCollection<string> existingNames);
}
