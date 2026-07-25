using AStar.Dev.FunctionalParadigm;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Represents a keyword-to-classification mapping used to classify files by matching keywords in their paths.</summary>
public sealed record KeywordMapping(string Keyword, string Level1, Option<string> Level2, Option<string> Level3, bool IsSpecial);
