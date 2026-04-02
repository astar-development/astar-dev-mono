using System.Reactive;
using AStar.Dev.OneDriveSync.Features.Accounts;
using AStar.Dev.OneDriveSync.Infrastructure;
using AStar.Dev.OneDriveSync.Infrastructure.Shell;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.Features.Onboarding;

public sealed class OnboardingViewModel : ViewModelBase
{
    private readonly IAccountRepository _accountRepository;

    public OnboardingViewModel(IAccountRepository accountRepository, IShellNavigator shellNavigator)
    {
        _accountRepository = accountRepository;

        AddAccountCommand = ReactiveCommand.Create(() => shellNavigator.Navigate(NavSection.Accounts));
    }

    public async Task<bool> ShouldShowOnboarding() => !await _accountRepository.HasAnyAsync().ConfigureAwait(false);

    public ReactiveCommand<Unit, Unit> AddAccountCommand { get; }
}
