using System.Reactive;
using AStar.Dev.OneDriveSync.Features.Accounts;
using AStar.Dev.OneDriveSync.Infrastructure;
using AStar.Dev.OneDriveSync.Infrastructure.Shell;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.Features.Onboarding;

public sealed class OnboardingViewModel : ViewModelBase
{
    private readonly IAccountRepository _accountRepository;
    private bool? _shouldShowOnboarding;

    public OnboardingViewModel(IAccountRepository accountRepository, IShellNavigator shellNavigator)
    {
        _accountRepository = accountRepository;

        AddAccountCommand = ReactiveCommand.Create(() => shellNavigator.Navigate(NavSection.Accounts));
    }

    public bool ShouldShowOnboarding
    {
        get
        {
            _shouldShowOnboarding ??= !_accountRepository.HasAnyAsync().GetAwaiter().GetResult();

            return _shouldShowOnboarding.Value;
        }
    }

    public ReactiveCommand<Unit, Unit> AddAccountCommand { get; }
}
