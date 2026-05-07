using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Sync.Client.Home;

/// <summary>Represents a OneDrive drive ID, providing type safety over raw strings.</summary>
/// <param name="Value">The string value of the drive ID.</param>
public readonly record struct DriveId(string Value);

/// <summary>Factory for <see cref="DriveId"/>.</summary>
public static class DriveIdFactory
{
    /// <summary>Creates an <see cref="Option{T}"/> of <see cref="DriveId"/> from a string. Returns <see cref="Option{T}.None"/> when the value is null or empty.</summary>
    public static Option<DriveId> Create(string value)
        => string.IsNullOrEmpty(value) ? Option<DriveId>.None.Instance : new Option<DriveId>.Some(new DriveId(value));

    /// <summary>An empty <see cref="DriveId"/> option representing the absence of a drive ID.</summary>
    public static Option<DriveId> Empty => Option<DriveId>.None.Instance;
}
