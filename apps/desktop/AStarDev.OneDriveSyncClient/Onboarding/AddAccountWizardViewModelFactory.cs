using AStarDev.OneDriveSyncClient.Infrastructure.Authentication;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;
using AStarDev.Localization;

namespace AStarDev.OneDriveSyncClient.Onboarding;

/// <summary>Container-backed factory for <see cref="AddAccountWizardViewModel"/> instances.</summary>
public sealed class AddAccountWizardViewModelFactory(IAuthService authService, IGraphService graphService, ILocalizationService localizationService) : IAddAccountWizardViewModelFactory
{
    /// <inheritdoc />
    public AddAccountWizardViewModel Create() => new(authService, graphService, localizationService);
}
