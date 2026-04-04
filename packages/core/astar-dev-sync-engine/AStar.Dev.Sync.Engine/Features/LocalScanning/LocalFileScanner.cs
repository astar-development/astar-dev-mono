using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using AStar.Dev.Sync.Engine.Infrastructure;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.Sync.Engine.Features.LocalScanning;

/// <inheritdoc />
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Registered via DI in SyncEngineServiceExtensions.")]
internal sealed class LocalFileScanner(IFileSystem fileSystem, ILogger<LocalFileScanner> logger) : ILocalFileScanner
{
    private const string GitDirectoryName = ".git";

    /// <inheritdoc />
    public async IAsyncEnumerable<string> ScanAsync(string rootPath, [EnumeratorCancellation] CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        var directories = new Stack<string>();
        directories.Push(rootPath);

        while (directories.Count > 0)
        {
            ct.ThrowIfCancellationRequested();

            var current = directories.Pop();

            if (ShouldSkipDirectory(current))
            {
                SyncEngineLogMessage.SkippedDirectory(logger, current);
                continue;
            }

            foreach (var file in fileSystem.Directory.EnumerateFiles(current))
            {
                ct.ThrowIfCancellationRequested();

                if (ShouldSkipFile(file))
                {
                    SyncEngineLogMessage.SkippedFile(logger, file);
                    continue;
                }

                yield return file;
                await Task.Yield();
            }

            foreach (var subdirectory in fileSystem.Directory.EnumerateDirectories(current))
                directories.Push(subdirectory);
        }
    }

    private bool ShouldSkipDirectory(string directoryPath)
    {
        var name = fileSystem.Path.GetFileName(directoryPath);

        return string.Equals(name, GitDirectoryName, StringComparison.OrdinalIgnoreCase);
    }

    private bool ShouldSkipFile(string filePath)
    {
        var fileInfo = fileSystem.FileInfo.New(filePath);

        var isSymlink = (fileInfo.Attributes & FileAttributes.ReparsePoint) != 0;

        return isSymlink;
    }
}
