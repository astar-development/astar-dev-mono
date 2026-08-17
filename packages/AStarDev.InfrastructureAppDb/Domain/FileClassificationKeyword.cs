using AStar.Dev.FunctionalParadigm;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>A keyword used to match file paths against a classification rule.</summary>
public sealed record FileClassificationKeyword(string Value, Option<bool> IsFamous, Option<bool> IsInternet);
