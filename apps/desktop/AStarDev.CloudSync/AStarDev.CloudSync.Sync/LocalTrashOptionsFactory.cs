namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="LocalTrashOptions"/> values.</summary>
public static class LocalTrashOptionsFactory
{
    private static readonly TimeSpan DefaultRetention = TimeSpan.FromDays(30);

    /// <summary>Creates the default: items stay 30 days.</summary>
    public static LocalTrashOptions Create() => new(DefaultRetention);

    /// <summary>Creates options. A negative retention becomes zero, so everything in the trash is removed at the next pass.</summary>
    /// <param name="retention">How long a trashed item is kept.</param>
    public static LocalTrashOptions Create(TimeSpan retention) => new(retention < TimeSpan.Zero ? TimeSpan.Zero : retention);
}
