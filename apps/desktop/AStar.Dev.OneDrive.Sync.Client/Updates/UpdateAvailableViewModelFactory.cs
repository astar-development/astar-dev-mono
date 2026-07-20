using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Updates;
using Microsoft.Extensions.Logging;
using Velopack;

namespace AStar.Dev.OneDrive.Sync.Client.Updates;

/// <inheritdoc cref="IUpdateAvailableViewModelFactory" />
public sealed class UpdateAvailableViewModelFactory(IUpdateCheckService updateCheckService, ILogger<UpdateAvailableViewModel> logger) : IUpdateAvailableViewModelFactory
{
    /// <inheritdoc />
    public UpdateAvailableViewModel Create(UpdateInfo updateInfo) => new(updateInfo, updateCheckService, logger);
}
