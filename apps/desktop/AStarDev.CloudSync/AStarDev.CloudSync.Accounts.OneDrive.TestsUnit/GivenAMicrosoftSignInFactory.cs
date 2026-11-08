
namespace AStarDev.CloudSync.Accounts.OneDrive.TestsUnit;

public class GivenAMicrosoftSignInFactory
{
    [Fact]
    public void when_values_are_missing_then_they_become_empty()
    {
        var signIn = MicrosoftSignInFactory.Create(null, null, null, null, null);

        (signIn.AccountKey, signIn.Email, signIn.DisplayName, signIn.AccessToken, signIn.GrantedScopes.Count).ShouldBe((string.Empty, string.Empty, string.Empty, string.Empty, 0));
    }

    [Fact]
    public void when_a_sign_in_is_created_then_its_values_are_kept()
    {
        var signIn = MicrosoftSignInFactory.Create(TestSignIns.AccountKey, TestSignIns.Email, TestSignIns.DisplayName, TestSignIns.AccessToken, TestSignIns.GrantedScopes);

        (signIn.AccountKey, signIn.Email, signIn.DisplayName, signIn.AccessToken).ShouldBe((TestSignIns.AccountKey, TestSignIns.Email, TestSignIns.DisplayName, TestSignIns.AccessToken));
        signIn.GrantedScopes.ShouldBe(TestSignIns.GrantedScopes);
    }

    [Fact]
    public void when_a_sign_in_is_written_as_text_then_no_account_details_or_token_appear() => TestSignIns.NewAccount.ToString().ShouldBe("MicrosoftSignIn(redacted)");
}
