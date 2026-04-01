using AStar.Dev.OneDriveSync.Features.Accounts;
using AStar.Dev.OneDriveSync.Infrastructure;

namespace AStar.Dev.OneDriveSync.Features.Onboarding;

public sealed class OnboardingViewModel : ViewModelBase
{
    private bool _shouldShowOnboarding;

    private OnboardingViewModel() { }

    public bool ShouldShowOnboarding => _shouldShowOnboarding;

    public static async Task<OnboardingViewModel> CreateAsync(IAccountRepository accountRepository, CancellationToken ct = default)
    {
        var vm = new OnboardingViewModel();
        vm._shouldShowOnboarding = !await accountRepository.HasAnyAsync().ConfigureAwait(false);

        return vm;
    }
}
