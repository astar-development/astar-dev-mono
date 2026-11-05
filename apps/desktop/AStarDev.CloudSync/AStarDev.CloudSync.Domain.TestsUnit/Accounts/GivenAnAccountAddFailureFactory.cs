using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Domain.Accounts;

namespace AStarDev.CloudSync.Domain.TestsUnit.Accounts;

public class GivenAnAccountAddFailureFactory
{
    public static TheoryData<Func<AccountAddFailure>, Type> FailureFactoriesAndCases() => new()
    {
        { AccountAddFailureFactory.CreateBrowserUnavailable, typeof(AccountAddFailure.BrowserUnavailable) },
        { AccountAddFailureFactory.CreateAuthRejected, typeof(AccountAddFailure.AuthRejected) },
        { AccountAddFailureFactory.CreateNetwork, typeof(AccountAddFailure.Network) },
        { AccountAddFailureFactory.CreateTokenCacheUnavailable, typeof(AccountAddFailure.TokenCacheUnavailable) },
        { AccountAddFailureFactory.CreateDriveUnavailable, typeof(AccountAddFailure.DriveUnavailable) },
        { AccountAddFailureFactory.CreatePersistFailed, typeof(AccountAddFailure.PersistFailed) },
    };

    [Theory]
    [MemberData(nameof(FailureFactoriesAndCases))]
    public void when_a_failure_is_created_then_it_is_the_matching_case(Func<AccountAddFailure> createFailure, Type expectedCase)
    {
        var failure = createFailure();

        failure.ShouldBeOfType(expectedCase);
    }
}
