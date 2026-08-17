namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>Creates <see cref="FileHandle" /> instances, normalizing invalid input instead of throwing.</summary>
public static class FileHandleFactory
{
    /// <summary>Creates a <see cref="FileHandle" /> from <paramref name="value" />, falling back to a new unique value when it is null or whitespace.</summary>
    public static FileHandle Create(string value) => string.IsNullOrWhiteSpace(value) ? new FileHandle(Guid.CreateVersion7().ToString()) : new FileHandle(value);
}
