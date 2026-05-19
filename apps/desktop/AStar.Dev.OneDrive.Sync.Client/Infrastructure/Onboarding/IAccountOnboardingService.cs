using AStar.Dev.OneDrive.Sync.Client.Accounts;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Onboarding;

/// <summary>Handles persistence and configuration during account onboarding.</summary>
public interface IAccountOnboardingService
{
    /// <summary>
    /// Persists the new account, resolves a default sync path when none is configured,
    /// writes sync rules for each selected folder, and marks the account active when appropriate.
    /// Returns the finalised <see cref="OneDriveAccount"/>.
    /// </summary>
    Task<OneDriveAccount> CompleteOnboardingAsync(OneDriveAccount account, CancellationToken ct);
}
