using AStar.Dev.OneDriveSync.Features.Accounts;
using AStar.Dev.OneDriveSync.Infrastructure;

namespace AStar.Dev.OneDriveSync.Features.Onboarding;

public sealed class OnboardingViewModel(IAccountRepository accountRepository) : ViewModelBase
{
    private readonly IAccountRepository _accountRepository = accountRepository;
    private bool? _shouldShowOnboarding;

    public bool ShouldShowOnboarding
    {
        get
        {
            _shouldShowOnboarding ??= !_accountRepository.HasAnyAsync().GetAwaiter().GetResult();

            return _shouldShowOnboarding.Value;
        }
    }
}

