namespace AStar.Dev.File.App.Services;

public interface IFileDeleteService
{
    Task DeleteFileAsync(string filePath, bool moveToRecycleBin = true);
    Task DeleteFilesAsync(IEnumerable<string> filePaths, bool moveToRecycleBin = true);
}
