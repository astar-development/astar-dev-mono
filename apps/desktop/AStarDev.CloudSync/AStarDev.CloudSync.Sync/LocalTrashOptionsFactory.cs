namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="LocalTrashOptions"/> values.</summary>
public static class LocalTrashOptionsFactory
{
    private const int MaxDays = 365;

    private static readonly TimeSpan DefaultRetention = TimeSpan.FromDays(30);

    /// <summary>Creates the default: items stay 30 days.</summary>
    public static LocalTrashOptions Create() => new(DefaultRetention);

    /// <summary>Creates options. A negative retention becomes zero, so everything in the trash is removed at the next pass.</summary>
    /// <param name="retention">How long a trashed item is kept.</param>
    public static LocalTrashOptions Create(TimeSpan retention) => new(retention < TimeSpan.Zero ? TimeSpan.Zero : retention);

    /// <summary>The retentions, in days, the user can choose from (#1593).</summary>
    public static IReadOnlyList<int> Choices { get; } = [7, 14, 30, 90, 365];

    /// <summary>Creates options from a number of days. A value below one day or above 365 days is not accepted and becomes the default.</summary>
    /// <param name="days">How many days a trashed item is kept.</param>
    public static LocalTrashOptions CreateFromDays(int days) => days is >= 1 and <= MaxDays ? new(TimeSpan.FromDays(days)) : Create();
}
