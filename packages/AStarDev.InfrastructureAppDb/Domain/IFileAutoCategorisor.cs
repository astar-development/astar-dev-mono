using AStar.Dev.FunctionalParadigm;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Derives a <see cref="FileClassification"/> from a full remote file path.</summary>
public interface IFileAutoCategorisor
{
    /// <summary>Derives a <see cref="FileClassification"/> from a full remote file path, or <see cref="Option.None{T}"/> when no meaningful classification can be determined.</summary>
    Option<FileClassification> Categorise(string remotePath);
}
