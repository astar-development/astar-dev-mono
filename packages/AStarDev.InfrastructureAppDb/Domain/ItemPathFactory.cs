using AStar.Dev.FunctionalParadigm;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Factory for <see cref="ItemPath"/>.</summary>
public static class ItemPathFactory
{
    /// <summary>Creates an <see cref="ItemPath"/> with no relative path.</summary>
    public static ItemPath Create(string name) => new(name, Option.None<string>());

    /// <summary>Creates an <see cref="ItemPath"/> with the given name and relative path.</summary>
    public static ItemPath Create(string name, Option<string> relativePath) => new(name, relativePath);
}
