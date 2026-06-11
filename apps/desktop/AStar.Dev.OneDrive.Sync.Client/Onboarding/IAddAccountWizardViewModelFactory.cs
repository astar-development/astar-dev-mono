namespace AStar.Dev.OneDrive.Sync.Client.Onboarding;

/// <summary>Creates <see cref="AddAccountWizardViewModel"/> instances with their service dependencies resolved from the container.</summary>
public interface IAddAccountWizardViewModelFactory
{
    /// <summary>Creates a new add-account wizard view model.</summary>
    AddAccountWizardViewModel Create();
}
