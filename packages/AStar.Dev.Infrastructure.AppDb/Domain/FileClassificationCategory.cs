using System.Diagnostics;
using AStar.Dev.FunctionalParadigm;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>A named category node in the file classification hierarchy (Level 1+).</summary>
[DebuggerDisplay("{Name} (Level {Level})")]
public sealed record FileClassificationCategory(FileClassificationCategoryId Id, string Name, int Level, bool IsFamous, bool IsInternet, Option<FileClassificationCategoryId> ParentId, bool IncludeInSearch);
