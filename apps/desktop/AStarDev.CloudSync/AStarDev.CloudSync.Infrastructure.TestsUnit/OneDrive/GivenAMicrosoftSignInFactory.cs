using AStarDev.CloudSync.Infrastructure.OneDrive;

namespace AStarDev.CloudSync.Infrastructure.TestsUnit.OneDrive;

public class GivenAMicrosoftSignInFactory
{
    [Fact]
    public void when_values_are_missing_then_they_become_empty_strings() => MicrosoftSignInFactory.Create(null, null, null, null).ShouldBe(new MicrosoftSignIn(string.Empty, string.Empty, string.Empty, string.Empty));

    [Fact]
    public void when_a_sign_in_is_created_then_its_values_are_kept() => MicrosoftSignInFactory.Create(TestSignIns.AccountKey, TestSignIns.Email, TestSignIns.DisplayName, TestSignIns.AccessToken).ShouldBe(new MicrosoftSignIn(TestSignIns.AccountKey, TestSignIns.Email, TestSignIns.DisplayName, TestSignIns.AccessToken));

    [Fact]
    public void when_a_sign_in_is_written_as_text_then_no_account_details_or_token_appear() => TestSignIns.NewAccount.ToString().ShouldBe("MicrosoftSignIn(redacted)");
}
