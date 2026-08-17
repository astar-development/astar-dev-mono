using AStarDev.OneDriveSyncClient.Infrastructure.Authentication;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;
using AStarDev.OneDriveSyncClient.Localization;
using AStarDev.OneDriveSyncClient.Onboarding;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Onboarding;

public sealed class GivenAnAddAccountWizardViewModelFactory
{
    private static AddAccountWizardViewModelFactory CreateSut() => new(Substitute.For<IAuthService>(), Substitute.For<IGraphService>(), Substitute.For<ILocalizationService>());

    [Fact]
    public void when_create_is_called_then_a_wizard_view_model_is_returned()
    {
        var sut = CreateSut();

        var wizard = sut.Create();

        wizard.ShouldNotBeNull();
    }

    [Fact]
    public void when_create_is_called_twice_then_distinct_instances_are_returned()
    {
        var sut = CreateSut();

        var firstWizard = sut.Create();
        var secondWizard = sut.Create();

        firstWizard.ShouldNotBeSameAs(secondWizard);
    }
}
