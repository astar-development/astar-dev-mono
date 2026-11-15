using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Accounts.TestsUnit;

public class GivenAnAccountAddFailureFactory
{
    private static readonly Dictionary<string, (Func<AccountAddFailure> Create, Type ExpectedCase)> Cases = new()
    {
        ["BrowserUnavailable"] = (AccountAddFailureFactory.CreateBrowserUnavailable, typeof(AccountAddFailure.BrowserUnavailable)),
        ["AuthRejected"] = (AccountAddFailureFactory.CreateAuthRejected, typeof(AccountAddFailure.AuthRejected)),
        ["Network"] = (AccountAddFailureFactory.CreateNetwork, typeof(AccountAddFailure.Network)),
        ["TokenCacheUnavailable"] = (AccountAddFailureFactory.CreateTokenCacheUnavailable, typeof(AccountAddFailure.TokenCacheUnavailable)),
        ["DriveUnavailable"] = (AccountAddFailureFactory.CreateDriveUnavailable, typeof(AccountAddFailure.DriveUnavailable)),
        ["PersistFailed"] = (AccountAddFailureFactory.CreatePersistFailed, typeof(AccountAddFailure.PersistFailed)),
    };

    [Theory]
    [InlineData("BrowserUnavailable")]
    [InlineData("AuthRejected")]
    [InlineData("Network")]
    [InlineData("TokenCacheUnavailable")]
    [InlineData("DriveUnavailable")]
    [InlineData("PersistFailed")]
    public void when_a_failure_is_created_then_it_is_the_matching_case(string caseName)
    {
        var (create, expectedCase) = Cases[caseName];

        var failure = create();

        failure.ShouldBeOfType(expectedCase);
    }
}
