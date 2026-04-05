using AStar.Dev.OneDrive.Client.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models.ODataErrors;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.Infrastructure;

public sealed class GivenAGraphRetryHelper
{
    private readonly ILogger _logger = Substitute.For<ILogger>();

    [Fact]
    public async Task when_first_call_throttled_second_call_succeeds_then_returns_result()
    {
        var callCount = 0;

        var result = await GraphRetryHelper.CallWithRetryAsync(() =>
        {
            callCount++;
            if (callCount == 1)
                throw new ODataError { ResponseStatusCode = 429 };

            return Task.FromResult("success");
        }, _logger, TestContext.Current.CancellationToken);

        result.ShouldBe("success");
    }

    [Fact]
    public async Task when_retry_after_header_present_then_delay_is_read_from_header()
    {
        var callCount = 0;

        var result = await GraphRetryHelper.CallWithRetryAsync(() =>
        {
            callCount++;
            if (callCount == 1)
            {
                var error = new ODataError { ResponseStatusCode = 429 };
                error.ResponseHeaders.Add("Retry-After", ["0"]);
                throw error;
            }

            return Task.FromResult("success-with-header");
        }, _logger, TestContext.Current.CancellationToken);

        result.ShouldBe("success-with-header");
    }

    [Fact]
    public async Task when_all_retries_exhausted_with_429_then_rethrows_odata_error()
    {
        var error = new ODataError { ResponseStatusCode = 429 };
        var callCount = 0;

        Task<string> AlwaysThrottle()
        {
            callCount++;
            throw error;
        }

        var thrown = await Should.ThrowAsync<ODataError>(() =>
            GraphRetryHelper.CallWithRetryAsync(AlwaysThrottle, _logger, TestContext.Current.CancellationToken));

        thrown.ResponseStatusCode.ShouldBe(429);
        callCount.ShouldBe(GraphRetryHelper.MaxRetries);
    }
}
