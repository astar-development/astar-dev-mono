using AStar.Dev.OneDriveSync.Features.Accounts;
using AStar.Dev.OneDriveSync.Features.Onboarding;
using NSubstitute;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Features.Onboarding;

public sealed class GivenNoAccountsInDatabase
{
    [Fact]
    public async Task when_onboarding_view_model_is_created_then_should_show_onboarding_is_true()
    {
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.HasAnyAsync().Returns(false);

        var sut = new OnboardingViewModel(accountRepository);

        sut.ShouldShowOnboarding.ShouldBeTrue();
    }

    [Fact]
    public async Task when_one_or_more_accounts_exist_then_should_show_onboarding_is_false()
    {
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.HasAnyAsync().Returns(true);

        var sut = new OnboardingViewModel(accountRepository);

        sut.ShouldShowOnboarding.ShouldBeFalse();
    }
}
