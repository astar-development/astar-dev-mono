namespace AStarDev.LoggingExtensions.TestsUnit;

public sealed class GivenTheErrorTypes
{
    [Fact]
    public void when_an_exception_is_described_then_its_full_type_name_is_used()
        => ErrorTypes.From(new InvalidOperationException("ada@example.com")).ShouldBe("System.InvalidOperationException");

    [Fact]
    public void when_a_timeout_exception_is_described_then_timeout_is_used()
        => ErrorTypes.From(new TimeoutException()).ShouldBe("timeout");

    [Fact]
    public void when_an_http_client_timeout_is_described_then_timeout_is_used()
        => ErrorTypes.From(new TaskCanceledException("The request was canceled", new TimeoutException())).ShouldBe("timeout");

    [Fact]
    public void when_a_plain_cancellation_is_described_then_its_full_type_name_is_used()
        => ErrorTypes.From(new TaskCanceledException()).ShouldBe("System.Threading.Tasks.TaskCanceledException");

    [Fact]
    public void when_an_http_status_code_is_described_then_it_is_used_as_text()
        => ErrorTypes.FromStatusCode(503).ShouldBe("503");

    [Fact]
    public void when_nothing_fits_then_the_other_fallback_is_used()
        => ErrorTypes.Other.ShouldBe("_OTHER");
}
