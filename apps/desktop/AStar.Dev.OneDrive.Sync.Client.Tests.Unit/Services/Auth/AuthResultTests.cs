using AStar.Dev.OneDrive.Sync.Client.Services;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Auth;

public class AuthResultTests
{
    [Fact]
    public void Success_ShouldCreateResultWithCorrectProperties()
    {
        string accessToken = "access-token-abc123";
        string accountId = "account-123";
        string displayName = "Jason Smith";
        string email = "jason@outlook.com";

        var result = AuthResult.Success(accessToken, accountId, displayName, email);

        result.IsSuccess.ShouldBeTrue();
        result.IsCancelled.ShouldBeFalse();
        result.IsError.ShouldBeFalse();
        result.AccessToken.ShouldBe(accessToken);
        result.AccountId.ShouldBe(accountId);
        result.DisplayName.ShouldBe(displayName);
        result.Email.ShouldBe(email);
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void Cancelled_ShouldCreateCancelledResult()
    {
        var result = AuthResult.Cancelled();

        result.IsCancelled.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
        result.IsError.ShouldBeFalse();
        result.AccessToken.ShouldBeNull();
        result.AccountId.ShouldBeNull();
        result.DisplayName.ShouldBeNull();
        result.Email.ShouldBeNull();
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void Failure_ShouldCreateFailureResultWithErrorMessage()
    {
        string errorMessage = "Authentication failed: Invalid credentials";

        var result = AuthResult.Failure(errorMessage);

        result.IsError.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
        result.IsCancelled.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(errorMessage);
        result.AccessToken.ShouldBeNull();
        result.AccountId.ShouldBeNull();
        result.DisplayName.ShouldBeNull();
        result.Email.ShouldBeNull();
    }

    [Theory]
    [InlineData("token-1", "account-1", "User One", "user1@outlook.com")]
    [InlineData("token-2", "account-2", "User Two", "user2@outlook.com")]
    [InlineData("token-3", "account-3", "User Three", "user3@outlook.com")]
    public void Success_ShouldPreserveDifferentTokenAndAccountData(
        string accessToken,
        string accountId,
        string displayName,
        string email)
    {
        var result = AuthResult.Success(accessToken, accountId, displayName, email);

        result.AccessToken.ShouldBe(accessToken);
        result.AccountId.ShouldBe(accountId);
        result.DisplayName.ShouldBe(displayName);
        result.Email.ShouldBe(email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Simple error")]
    [InlineData("Authentication failed: Invalid credentials")]
    [InlineData("Network error: Connection timeout")]
    public void Failure_ShouldPreserveDifferentErrorMessages(string errorMessage)
    {
        var result = AuthResult.Failure(errorMessage);

        result.ErrorMessage.ShouldBe(errorMessage);
        result.IsError.ShouldBeTrue();
    }

    [Fact]
    public void IsError_ShouldBeTrueWhenNotSuccessAndNotCancelled()
    {
        var result = AuthResult.Failure("Some error");

        result.IsError.ShouldBeTrue();
    }

    [Fact]
    public void IsError_ShouldBeFalseWhenSuccess()
    {
        var result = AuthResult.Success("token", "account", "name", "email@test.com");

        result.IsError.ShouldBeFalse();
    }

    [Fact]
    public void IsError_ShouldBeFalseWhenCancelled()
    {
        var result = AuthResult.Cancelled();

        result.IsError.ShouldBeFalse();
    }

    [Fact]
    public void SuccessResult_ShouldNotBeEqual_ToFailureResult()
    {
        var successResult = AuthResult.Success("token", "account", "name", "email@test.com");
        var failureResult = AuthResult.Failure("error message");

        successResult.IsSuccess.ShouldNotBe(failureResult.IsSuccess);
    }

    [Fact]
    public void CancelledResult_ShouldNotBeEqual_ToFailureResult()
    {
        var cancelledResult = AuthResult.Cancelled();
        var failureResult = AuthResult.Failure("error message");

        cancelledResult.IsCancelled.ShouldNotBe(failureResult.IsCancelled);
    }

    [Fact]
    public void SuccessResult_ShouldNotBeEqual_ToCancelledResult()
    {
        var successResult = AuthResult.Success("token", "account", "name", "email@test.com");
        var cancelledResult = AuthResult.Cancelled();

        successResult.IsSuccess.ShouldNotBe(cancelledResult.IsSuccess);
    }
}
