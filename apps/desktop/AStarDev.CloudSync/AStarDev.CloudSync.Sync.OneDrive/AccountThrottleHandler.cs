using System.Net;
using System.Net.Http.Headers;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.OneDrive;

/// <summary>Waits on <see cref="IAccountThrottleGate"/> before each account request and reports 429 / 503 (with Retry-After) and successes after it. It does not retry; the caller decides.</summary>
/// <param name="gate">The per-account throttle gate.</param>
/// <param name="timeProvider">Converts a Retry-After date into a delay.</param>
internal sealed class AccountThrottleHandler(IAccountThrottleGate gate, TimeProvider timeProvider) : DelegatingHandler
{
    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!request.Options.TryGetValue(OneDriveSyncHttp.AccountOption, out var accountId))
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        await gate.WaitAsync(accountId, cancellationToken).ConfigureAwait(false);
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        Report(accountId, response);

        return response;
    }

    private void Report(AccountId accountId, HttpResponseMessage response)
    {
        if (response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable)
            gate.ReportThrottled(accountId, RetryAfter(response.Headers.RetryAfter));
        else if (response.IsSuccessStatusCode)
            gate.ReportSucceeded(accountId);
    }

    private Option<TimeSpan> RetryAfter(RetryConditionHeaderValue? header) => header switch
    {
        { Delta: { } delta } => Option.Some(delta),
        { Date: { } date } => Option.Some(date - timeProvider.GetUtcNow()),
        _ => Option.None<TimeSpan>(),
    };
}
