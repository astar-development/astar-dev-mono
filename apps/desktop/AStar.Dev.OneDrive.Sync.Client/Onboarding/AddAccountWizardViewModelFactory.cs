using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Localization;

namespace AStar.Dev.OneDrive.Sync.Client.Onboarding;

/// <summary>Container-backed factory for <see cref="AddAccountWizardViewModel"/> instances.</summary>
public sealed class AddAccountWizardViewModelFactory(IAuthService authService, IGraphService graphService, ILocalizationService localizationService) : IAddAccountWizardViewModelFactory
{
    /// <inheritdoc />
    public AddAccountWizardViewModel Create() => new(authService, graphService, localizationService);
}
