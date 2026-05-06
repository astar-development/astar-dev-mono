using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Authentication;

public sealed class GivenAnAuthResultFactory
{
    private const string AccessToken = "access-token-abc123";
    private const string AccountId = "account-123";
    private const string DisplayName = "Jason Smith";
    private const string Email = "jason@outlook.com";
    private const string ErrorMessage = "Authentication failed: Invalid credentials";

    private static AccountProfile Profile => AccountProfileFactory.Create(DisplayName, Email);

    [Fact]
    public void when_success_is_called_then_result_is_ok() =>
        AuthResultFactory.Success(AccessToken, AccountId, Profile)
            .ShouldBeOfType<Result<AuthResult, AuthError>.Ok>();

    [Fact]
    public void when_success_is_called_then_ok_value_has_correct_access_token()
    {
        var result = (Result<AuthResult, AuthError>.Ok)AuthResultFactory.Success(AccessToken, AccountId, Profile);

        result.Value.AccessToken.ShouldBe(AccessToken);
    }

    [Fact]
    public void when_success_is_called_then_ok_value_has_correct_account_id()
    {
        var result = (Result<AuthResult, AuthError>.Ok)AuthResultFactory.Success(AccessToken, AccountId, Profile);

        result.Value.AccountId.ShouldBe(AccountId);
    }

    [Fact]
    public void when_success_is_called_then_ok_value_has_correct_display_name()
    {
        var result = (Result<AuthResult, AuthError>.Ok)AuthResultFactory.Success(AccessToken, AccountId, Profile);

        result.Value.Profile.DisplayName.ShouldBe(DisplayName);
    }

    [Fact]
    public void when_success_is_called_then_ok_value_has_correct_email()
    {
        var result = (Result<AuthResult, AuthError>.Ok)AuthResultFactory.Success(AccessToken, AccountId, Profile);

        result.Value.Profile.Email.ShouldBe(Email);
    }

    [Fact]
    public void when_cancelled_is_called_then_result_is_error() =>
        AuthResultFactory.Cancelled()
            .ShouldBeOfType<Result<AuthResult, AuthError>.Error>();

    [Fact]
    public void when_cancelled_is_called_then_error_reason_is_auth_cancelled_error()
    {
        var result = (Result<AuthResult, AuthError>.Error)AuthResultFactory.Cancelled();

        result.Reason.ShouldBeOfType<AuthCancelledError>();
    }

    [Fact]
    public void when_failure_is_called_then_result_is_error() =>
        AuthResultFactory.Failure(ErrorMessage)
            .ShouldBeOfType<Result<AuthResult, AuthError>.Error>();

    [Fact]
    public void when_failure_is_called_then_error_reason_is_auth_failed_error()
    {
        var result = (Result<AuthResult, AuthError>.Error)AuthResultFactory.Failure(ErrorMessage);

        result.Reason.ShouldBeOfType<AuthFailedError>();
    }

    [Fact]
    public void when_failure_is_called_then_auth_failed_error_carries_the_message()
    {
        var result = (Result<AuthResult, AuthError>.Error)AuthResultFactory.Failure(ErrorMessage);
        var authFailed = (AuthFailedError)result.Reason;

        authFailed.Message.ShouldBe(ErrorMessage);
    }

    [Theory]
    [InlineData("token-1", "account-1", "User One", "user1@outlook.com")]
    [InlineData("token-2", "account-2", "User Two", "user2@outlook.com")]
    [InlineData("token-3", "account-3", "User Three", "user3@outlook.com")]
    public void when_success_is_called_then_all_token_and_account_data_are_preserved(string accessToken, string accountId, string displayName, string email)
    {
        var result = (Result<AuthResult, AuthError>.Ok)AuthResultFactory.Success(accessToken, accountId, AccountProfileFactory.Create(displayName, email));

        result.Value.AccessToken.ShouldBe(accessToken);
        result.Value.AccountId.ShouldBe(accountId);
        result.Value.Profile.DisplayName.ShouldBe(displayName);
        result.Value.Profile.Email.ShouldBe(email);
    }

    [Theory]
    [InlineData("Simple error")]
    [InlineData("Authentication failed: Invalid credentials")]
    [InlineData("Network error: Connection timeout")]
    public void when_failure_is_called_then_error_message_is_preserved(string errorMessage)
    {
        var result = (Result<AuthResult, AuthError>.Error)AuthResultFactory.Failure(errorMessage);
        var authFailed = (AuthFailedError)result.Reason;

        authFailed.Message.ShouldBe(errorMessage);
    }

    [Fact]
    public void when_success_and_failure_results_are_compared_then_they_are_different_subtypes()
    {
        var successResult = AuthResultFactory.Success(AccessToken, AccountId, Profile);
        var failureResult = AuthResultFactory.Failure(ErrorMessage);

        successResult.ShouldBeOfType<Result<AuthResult, AuthError>.Ok>();
        failureResult.ShouldBeOfType<Result<AuthResult, AuthError>.Error>();
    }

    [Fact]
    public void when_cancelled_and_failure_results_are_compared_then_their_error_reasons_differ()
    {
        var cancelledResult = (Result<AuthResult, AuthError>.Error)AuthResultFactory.Cancelled();
        var failureResult = (Result<AuthResult, AuthError>.Error)AuthResultFactory.Failure(ErrorMessage);

        cancelledResult.Reason.ShouldBeOfType<AuthCancelledError>();
        failureResult.Reason.ShouldBeOfType<AuthFailedError>();
    }
}
