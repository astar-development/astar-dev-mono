using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>A keyword used to match file paths against a classification rule.</summary>
public sealed record FileClassificationKeyword(string Value, Option<bool> IsFamous, Option<bool> IsInternet);
