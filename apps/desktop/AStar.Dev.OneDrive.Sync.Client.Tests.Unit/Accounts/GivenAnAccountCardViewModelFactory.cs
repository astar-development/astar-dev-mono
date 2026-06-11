using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Localization;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Accounts;

public sealed class GivenAnAccountCardViewModelFactory
{
    [Fact]
    public void when_create_is_called_then_the_card_reflects_the_account()
    {
        var sut = new AccountCardViewModelFactory(Substitute.For<ILocalizationService>());
        var account = new OneDriveAccount { Id = new AccountId("account-1"), Profile = AccountProfileFactory.Create("Test User", "user@example.com") };

        var card = sut.Create(account);

        card.Id.ShouldBe("account-1");
        card.Email.ShouldBe("user@example.com");
    }

    [Fact]
    public void when_create_is_called_twice_then_distinct_cards_are_returned()
    {
        var sut = new AccountCardViewModelFactory(Substitute.For<ILocalizationService>());
        var account = new OneDriveAccount { Id = new AccountId("account-1") };

        var firstCard = sut.Create(account);
        var secondCard = sut.Create(account);

        firstCard.ShouldNotBeSameAs(secondCard);
    }
}
