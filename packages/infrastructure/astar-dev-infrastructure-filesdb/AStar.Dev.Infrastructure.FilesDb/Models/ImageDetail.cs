using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Infrastructure.FilesDb.Models;

/// <summary>
/// </summary>
[Index(nameof(Width), nameof(Height))]
public record ImageDetail(int? Width, int? Height);