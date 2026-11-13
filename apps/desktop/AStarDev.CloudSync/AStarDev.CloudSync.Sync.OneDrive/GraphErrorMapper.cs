using System.Globalization;
using System.Net;
using AStarDev.FunctionalParadigm;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Kiota.Abstractions;

namespace AStarDev.CloudSync.Sync.OneDrive;

internal static class GraphErrorMapper
{
    private const string RetryAfterHeader = "Retry-After";
    private const string NoCode = "none";

    internal static (ProviderError Error, int StatusCode, string ErrorCode) Map(Exception exception, TimeProvider timeProvider) => exception switch
    {
        AggregateException { InnerExceptions.Count: > 0 } aggregate => Map(aggregate.InnerExceptions[^1], timeProvider),
        ApiException api => (FromStatus(api.ResponseStatusCode, RetryAfter(api, timeProvider)), api.ResponseStatusCode, CodeOf(api)),
        HttpRequestException or IOException or TaskCanceledException => (ProviderErrorFactory.CreateTransient(), 0, NoCode),
        _ => (ProviderErrorFactory.CreateUnexpected(), 0, NoCode),
    };

    internal static ProviderError FromStatus(int status, Option<TimeSpan> retryAfter) => status switch
    {
        (int)HttpStatusCode.Unauthorized => ProviderErrorFactory.CreateAuthenticationRequired(),
        (int)HttpStatusCode.Forbidden => ProviderErrorFactory.CreateAccessDenied(),
        (int)HttpStatusCode.NotFound => ProviderErrorFactory.CreateNotFound(),
        (int)HttpStatusCode.Conflict => ProviderErrorFactory.CreateNameConflict(),
        (int)HttpStatusCode.PreconditionFailed => ProviderErrorFactory.CreatePreconditionFailed(),
        (int)HttpStatusCode.RequestEntityTooLarge => ProviderErrorFactory.CreateFileTooLarge(),
        (int)HttpStatusCode.InsufficientStorage => ProviderErrorFactory.CreateQuotaExceeded(),
        (int)HttpStatusCode.TooManyRequests or (int)HttpStatusCode.ServiceUnavailable => ProviderErrorFactory.CreateThrottled(retryAfter),
        >= 500 => ProviderErrorFactory.CreateTransient(),
        _ => ProviderErrorFactory.CreateUnexpected(),
    };

    private static string CodeOf(ApiException exception) => exception is ODataError { Error.Code: { Length: > 0 } code } ? code : NoCode;

    private static Option<TimeSpan> RetryAfter(ApiException exception, TimeProvider timeProvider)
    {
        var value = exception.ResponseHeaders.TryGetValue(RetryAfterHeader, out var values) ? values.FirstOrDefault() : null;

        if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds))
            return Option.Some(TimeSpan.FromSeconds(seconds));

        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date)
            ? Option.Some(date - timeProvider.GetUtcNow())
            : Option.None<TimeSpan>();
    }
}
