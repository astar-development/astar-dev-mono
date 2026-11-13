using System.Globalization;

namespace AStarDev.CloudSync.Sync.OneDrive.TestsUnit;

public sealed class CultureScope : IDisposable
{
    private readonly CultureInfo previous = CultureInfo.CurrentCulture;

    public CultureScope(string name) => CultureInfo.CurrentCulture = new CultureInfo(name);

    public void Dispose() => CultureInfo.CurrentCulture = previous;
}
