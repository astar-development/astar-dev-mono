using System.IO.Abstractions;

namespace AStarDev.SingleInstance;

/// <summary>Creates <see cref="SingleInstanceOptions"/> values.</summary>
public static class SingleInstanceOptionsFactory
{
    /// <summary>The name used when none is given.</summary>
    public const string DefaultName = "instance";

    /// <summary>The hand-off timeout used when none, or a non-positive one, is given.</summary>
    public static readonly TimeSpan DefaultHandOffTimeout = TimeSpan.FromSeconds(3);

    /// <summary>Creates options. A blank name becomes <see cref="DefaultName"/> and a missing or non-positive timeout becomes <see cref="DefaultHandOffTimeout"/>.</summary>
    /// <param name="dataFolder">The per-user data folder.</param>
    /// <param name="name">The file name stem for the lock and the socket.</param>
    /// <param name="handOffTimeout">How long a second launch waits for an answer.</param>
    public static SingleInstanceOptions Create(IDirectoryInfo dataFolder, string? name, TimeSpan? handOffTimeout) =>
        new(dataFolder, string.IsNullOrWhiteSpace(name) ? DefaultName : name.Trim(), handOffTimeout is { } timeout && timeout > TimeSpan.Zero ? timeout : DefaultHandOffTimeout);
}
