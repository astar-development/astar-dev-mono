using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenACloudAccountIdentityFactory
{
    [Fact]
    public void when_the_text_is_missing_then_it_is_normalised_to_empty()
    {
        var identity = CloudAccountIdentityFactory.Create(AccountIdFactory.Create(Guid.NewGuid()), null!, null!, CloudProviderFactory.CreateOneDrive());

        (identity.DisplayName, identity.Email).ShouldBe((string.Empty, string.Empty));
    }
}
