using AStar.Dev.Functional.Extensions;
using System.IO.Abstractions;

namespace AStar.Dev.OneDriveSync.Features.Accounts;

internal sealed class LocalSyncPathService(IAccountRepository accountRepository, IFileSystem fileSystem) : ILocalSyncPathService
{
    public async Task<Result<bool, string>> ValidateNoOverlapAsync(string candidatePath, Guid? excludeAccountId = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(candidatePath);

        var normalised = NormalisePath(candidatePath);
        var existingPaths = await accountRepository.GetAllSyncPathsAsync(ct).ConfigureAwait(false);

        foreach (var (accountId, existingPath) in existingPaths)
        {
            if (excludeAccountId.HasValue && accountId == excludeAccountId.Value)
                continue;

            if (existingPath is null or { Length: 0 })
                continue;

            var normalisedExisting = NormalisePath(existingPath);

            if (PathsOverlap(normalised, normalisedExisting))
                return new Result<bool, string>.Error(
                    $"The selected folder overlaps with an existing account's sync folder: {existingPath}");
        }

        return new Result<bool, string>.Ok(true);
    }

    public bool IsNonEmpty(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!fileSystem.Directory.Exists(path))
            return false;

        return fileSystem.Directory.EnumerateFileSystemEntries(path).Any();
    }

    public string GetDefaultPath(string accountDisplayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountDisplayName);

        var sanitised = SanitisePathSegment(accountDisplayName);
        var home      = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return fileSystem.Path.Combine(home, "OneDrive", sanitised);
    }

    private static string NormalisePath(string path)
    {
        var trimmed = path.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);

        return trimmed + System.IO.Path.DirectorySeparatorChar;
    }

    private static bool PathsOverlap(string a, string b)
        => a.StartsWith(b, StringComparison.OrdinalIgnoreCase)
        || b.StartsWith(a, StringComparison.OrdinalIgnoreCase);

    private static string SanitisePathSegment(string name)
    {
        var invalid = System.IO.Path.GetInvalidFileNameChars();

        return string.Concat(name.Select(character => Array.IndexOf(invalid, character) >= 0 ? '_' : character));
    }
}
