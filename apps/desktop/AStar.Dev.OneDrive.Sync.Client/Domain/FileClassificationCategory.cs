using System.Diagnostics;
using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>A named category node in the file classification hierarchy (Level 1–3).</summary>
[DebuggerDisplay("{Name} (Level {Level})")]
public sealed record FileClassificationCategory(FileClassificationCategoryId Id, string Name, int Level, bool IsFamous, bool IsInternet, Option<FileClassificationCategoryId> ParentId);
