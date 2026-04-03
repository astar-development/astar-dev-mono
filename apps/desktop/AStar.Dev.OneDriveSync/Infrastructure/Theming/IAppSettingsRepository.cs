using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDriveSync.Infrastructure.Persistence;

namespace AStar.Dev.OneDriveSync.Infrastructure.Theming;

/// <summary>Provides read/write access to the single-row <see cref="AppSettings" /> record.</summary>
public interface IAppSettingsRepository
{
    /// <summary>Returns the stored settings, or <see langword="null" /> if none exist yet.</summary>
    Task<Result<AppSettings?, ErrorResponse>> GetAsync(CancellationToken ct = default);

    /// <summary>Upserts the settings record.</summary>
    Task<Result<AppSettings, ErrorResponse>> SaveAsync(AppSettings settings, CancellationToken ct = default);
}
