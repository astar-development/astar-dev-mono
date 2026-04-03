using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.OneDriveSync.Features.Accounts;
using AStar.Dev.OneDriveSync.Features.Onboarding;
using AStar.Dev.OneDriveSync.Infrastructure.Shell;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Features.Onboarding;

public sealed class GivenNoAccountsInDatabase
{
    [Fact]
    public async Task when_onboarding_view_model_is_created_then_should_show_onboarding_is_true()
    {
        var accountRepository = Substitute.For<IAccountRepository>();
        var shellNavigator    = Substitute.For<IShellNavigator>();
        accountRepository.HasAnyAsync(Arg.Any<CancellationToken>()).Returns(false);

        var sut = new OnboardingViewModel(accountRepository, shellNavigator);

        (await sut.ShouldShowOnboarding()).ShouldBeTrue();
    }

    [Fact]
    public async Task when_one_or_more_accounts_exist_then_should_show_onboarding_is_false()
    {
        var accountRepository = Substitute.For<IAccountRepository>();
        var shellNavigator    = Substitute.For<IShellNavigator>();
        accountRepository.HasAnyAsync(Arg.Any<CancellationToken>()).Returns(true);

        var sut = new OnboardingViewModel(accountRepository, shellNavigator);

        (await sut.ShouldShowOnboarding()).ShouldBeFalse();
    }
}
