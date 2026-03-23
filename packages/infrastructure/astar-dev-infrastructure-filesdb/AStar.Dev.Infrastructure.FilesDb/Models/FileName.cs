using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Infrastructure.FilesDb.Models;

/// <summary>
/// </summary>
/// <param name="Value"></param>
[Index(nameof(Value))]
public record FileName(string Value)
{
    /// <summary>
    /// </summary>
    /// <returns></returns>
    public override string ToString()
        => Value;
}