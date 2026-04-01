using AStar.Dev.OneDriveSync.Features.Accounts;
using AStar.Dev.OneDriveSync.Features.Onboarding;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Features.Onboarding;

public sealed class GivenNoAccountsInDatabase
{
    [Fact]
    public async Task when_onboarding_view_model_is_created_then_should_show_onboarding_is_true()
    {
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.HasAnyAsync().Returns(false);

        var sut = await OnboardingViewModel.CreateAsync(accountRepository, TestContext.Current.CancellationToken);

        sut.ShouldShowOnboarding.ShouldBeTrue();
    }

    [Fact]
    public async Task when_one_or_more_accounts_exist_then_should_show_onboarding_is_false()
    {
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.HasAnyAsync().Returns(true);

        var sut = await OnboardingViewModel.CreateAsync(accountRepository, TestContext.Current.CancellationToken);

        sut.ShouldShowOnboarding.ShouldBeFalse();
    }
}
