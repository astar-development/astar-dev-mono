using System.Reactive;
using System.Threading.Tasks;
using AStar.Dev.OneDriveSync.Features.Accounts;
using AStar.Dev.OneDriveSync.Infrastructure;
using AStar.Dev.OneDriveSync.Infrastructure.Shell;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.Features.Onboarding;

public sealed class OnboardingViewModel(IAccountRepository accountRepository, IShellNavigator shellNavigator) : ViewModelBase
{
    public async Task<bool> ShouldShowOnboarding() => !await accountRepository.HasAnyAsync().ConfigureAwait(false);

    public ReactiveCommand<Unit, Unit> AddAccountCommand { get; } = ReactiveCommand.Create(() => shellNavigator.Navigate(NavSection.Accounts));
}
