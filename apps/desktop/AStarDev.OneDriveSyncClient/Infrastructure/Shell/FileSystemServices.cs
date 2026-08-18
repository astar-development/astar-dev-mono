using System.IO.Abstractions;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Shell;

/// <summary>
/// Encapsulates file system services.
/// </summary>
/// <param name="FileSystem">The file system abstraction.</param>
/// <param name="FileManagerService">The file manager service.</param>
public record FileSystemServices(IFileSystem FileSystem, IFileManagerService FileManagerService);
