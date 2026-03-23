using AStar.Dev.Api.Usage.Sdk;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.Auth.Extensions;

/// <summary>
/// </summary>
public sealed class JwtEvents(ILogger<JwtEvents> logger)
{
    /// <summary>
    /// </summary>
    /// <param name="send"></param>
    /// <param name="applicationName"></param>
    /// <returns></returns>
    public JwtBearerEvents AddJwtEvents(Send send, string applicationName) =>
        new()
        {
            OnAuthenticationFailed = async context => await LogAuthenticationFailureAsync(send, applicationName, context),
            OnForbidden            = async context => await LogAuthorisationFailedAsync(send, applicationName, context),
            OnTokenValidated       = ValidateUserIdIsPopulates,
            OnChallenge            = async context => await LogChallengeFailureAsync(send, applicationName, context)
        };

    private async Task LogAuthenticationFailureAsync(Send send, string applicationName, AuthenticationFailedContext context)
    {
        logger.LogError(context.Exception, "Authentication failed: {ErrorMessage}", context.Exception.Message);

        await send
            .SendUsageEventAsync(new(applicationName, context.Request.Path, context.Request.Method, 0, StatusCodes.Status401Unauthorized),
                                 CancellationToken.None);
    }

    private async Task LogAuthorisationFailedAsync(Send send, string applicationName, ForbiddenContext context)
    {
        logger.LogWarning("Authorisation failed: {ErrorMessage}", "Forbidden");

        await send
            .SendUsageEventAsync(new(applicationName, context.Request.Path, context.Request.Method, 0, StatusCodes.Status403Forbidden),
                                 CancellationToken.None);
    }

    private static Task ValidateUserIdIsPopulates(TokenValidatedContext context)
    {
        var claimsPrincipal = context.Principal;
        var userId          = claimsPrincipal?.FindFirst(t => t.Type == "name")?.Value;

        if(string.IsNullOrEmpty(userId))
        {
            context.Fail("User ID could not be found.");
        }

        return Task.CompletedTask;
    }

    private async Task LogChallengeFailureAsync(Send send, string applicationName, JwtBearerChallengeContext context)
    {
        logger.LogError(context.AuthenticateFailure, "Authentication failed during the OnChallenge. Error message: {ChallengeError} - {ChallengeDescription}", context.Error ?? "Not Available",
                        context.ErrorDescription ?? "No description available");

        await send
            .SendUsageEventAsync(new(applicationName, context.Request.Path, context.Request.Method, 0, StatusCodes.Status401Unauthorized),
                                 CancellationToken.None);
    }
}
