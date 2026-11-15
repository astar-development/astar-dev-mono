using Microsoft.Extensions.Logging;
using Velopack;

namespace AStar.Dev.Velopack.Publishing.Avalonia.Updates;

/// <inheritdoc cref="IUpdateAvailableViewModelFactory" />
public sealed class UpdateAvailableViewModelFactory(IVelopackUpdateService updateCheckService, IUpdateApplyGate applyGate, IUpdateDialogTextProvider textProvider, ILogger<UpdateAvailableViewModel> logger) : IUpdateAvailableViewModelFactory
{
    /// <inheritdoc />
    public UpdateAvailableViewModel Create(UpdateInfo updateInfo) => new(updateInfo, updateCheckService, applyGate, textProvider, logger);

    /// <inheritdoc />
    public UpdateAvailableViewModel CreateForced(UpdateInfo updateInfo) => new(updateInfo, updateCheckService, applyGate, textProvider, logger, UpdateActionFactory.CreateForceAfterWarning());
}
