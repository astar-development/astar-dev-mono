using AStar.Dev.OneDriveSync.Models;

namespace AStar.Dev.OneDriveSync.Services;

/// <summary>AM-01 → AM-08: Persists account configuration to disk.</summary>
public interface IAccountStore
{
    Task<IReadOnlyList<AccountRecord>> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(IReadOnlyList<AccountRecord> accounts, CancellationToken ct = default);
}
