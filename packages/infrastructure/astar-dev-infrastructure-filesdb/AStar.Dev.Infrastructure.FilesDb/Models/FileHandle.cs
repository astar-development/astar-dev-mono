namespace AStar.Dev.Infrastructure.FilesDb.Models;

/// <summary>
/// </summary>
/// <param name="Value"></param>
public readonly record struct FileHandle(string Value)
{
    /// <summary>
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static FileHandle Create(string value)
        => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(value)) : new FileHandle(value);

    /// <summary>
    /// </summary>
    /// <param name="d"></param>
    /// <returns></returns>
    public static implicit operator string(FileHandle d)
        => d.Value;
}