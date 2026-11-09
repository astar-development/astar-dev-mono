using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.LoggingExtensions;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenASyncLogScope
{
    [Fact]
    public void when_a_scope_begins_then_it_carries_the_hashed_account_id_and_not_the_id_itself()
    {
        var accountGuid = Guid.Parse("3f2b8c1e-5d43-4a7e-9e61-0c8a7b6d5e4f");
        var logger = new RecordingLogger<GivenASyncLogScope>();

        using var scope = SyncLogScope.Begin(logger, AccountIdFactory.Create(accountGuid));

        var values = logger.Scopes.ShouldHaveSingleItem();
        (values[SyncLogScope.AccountIdHashKey], values.Values.Any(value => value?.ToString()?.Contains(accountGuid.ToString(), StringComparison.OrdinalIgnoreCase) == true)).ShouldBe((LogHash.Of(accountGuid.ToString()), false));
    }
}
