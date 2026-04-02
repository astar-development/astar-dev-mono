using AStar.Dev.File.App.Models;

namespace AStar.Dev.File.App.Services;

public interface IFileTypeClassifier
{
    FileType Classify(string fileExtension);
}
