using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;

namespace AStarDev.ScraperPlaying.TestsUnit.Scraping;

public sealed class GivenAJsonResponseProcessor
{
    private readonly JsonResponseProcessor processor = new();

    [Fact]
    public async Task when_the_response_is_successful_and_the_body_deserializes_then_a_success_is_returned()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"name":"cats"}""", Encoding.UTF8, "application/json")
        });

        var result = await processor.GetFromJsonAsync<TestPayload>(new Uri("https://example.test/search"), client, CancellationToken.None);

        result.Match(option => option.Match(value => value.Name, () => (string?)null), ex => ex.Message).ShouldBe("cats");
    }

    [Fact]
    public async Task when_the_url_is_relative_then_the_request_is_sent_to_the_client_base_address_combined_with_it()
    {
        var requested = new List<Uri>();
        using var client = CreateClient(request =>
        {
            requested.Add(request.RequestUri!);

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"name":"cats"}""", Encoding.UTF8, "application/json") };
        });
        client.BaseAddress = new Uri("https://example.test/");

        _ = await processor.GetFromJsonAsync<TestPayload>(new Uri("api/v1/search?page=2", UriKind.Relative), client, CancellationToken.None);

        requested.ShouldBe([new Uri("https://example.test/api/v1/search?page=2")]);
    }

    [Fact]
    public async Task when_the_response_is_successful_and_the_body_deserializes_to_null_then_a_success_wrapping_none_is_returned()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        });

        var result = await processor.GetFromJsonAsync<TestPayload>(new Uri("https://example.test/search"), client, CancellationToken.None);

        result.Match(option => option.Match(_ => false, () => true), _ => false).ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_response_status_is_not_successful_then_a_failure_is_returned_without_throwing()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("not found")
        });

        var result = await processor.GetFromJsonAsync<TestPayload>(new Uri("https://example.test/search"), client, CancellationToken.None);

        var capturedException = result.Match(_ => (Exception?)null, ex => ex);
        capturedException.ShouldBeOfType<HttpRequestException>();
        capturedException!.Message.ShouldContain("404");
        capturedException.Message.ShouldContain("https://example.test/search");
    }

    [Fact]
    public async Task when_the_response_body_cannot_be_deserialized_then_a_failure_is_returned_without_throwing()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not json", Encoding.UTF8, "application/json")
        });

        var result = await processor.GetFromJsonAsync<TestPayload>(new Uri("https://example.test/search"), client, CancellationToken.None);

        var capturedException = result.Match(_ => (Exception?)null, ex => ex);
        capturedException.ShouldBeOfType<InvalidOperationException>();
        capturedException!.Message.ShouldContain(nameof(TestPayload));
    }

    [Fact]
    public async Task when_the_cancellation_token_is_already_cancelled_then_the_operation_is_cancelled_not_captured()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK));

        await Should.ThrowAsync<OperationCanceledException>(
            () => processor.GetFromJsonAsync<TestPayload>(new Uri("https://example.test/search"), client, cancellationTokenSource.Token));
    }

    [Fact]
    public async Task when_the_request_times_out_without_a_cancellation_then_a_timeout_failure_is_returned_without_throwing()
    {
        using var client = CreateClient(_ => throw new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout."));

        var result = await processor.GetFromJsonAsync<TestPayload>(new Uri("https://example.test/search"), client, CancellationToken.None);

        result.Match(_ => (Exception?)null, ex => ex).ShouldBeOfType<TimeoutException>();
    }

    [Fact]
    public async Task when_an_error_response_has_a_short_body_then_the_whole_body_is_in_the_failure()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new StringContent("slow down") });

        var result = await processor.GetFromJsonAsync<TestPayload>(new Uri("https://example.test/search"), client, CancellationToken.None);

        result.Match(_ => string.Empty, ex => ex.Message).ShouldEndWith("Response: slow down");
    }

    [Fact]
    public async Task when_an_error_response_has_a_long_body_then_only_the_start_of_it_is_in_the_failure()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = new StringContent(new string('a', 5_000)) });

        var result = await processor.GetFromJsonAsync<TestPayload>(new Uri("https://example.test/search"), client, CancellationToken.None);

        var message = result.Match(_ => string.Empty, ex => ex.Message);
        message.ShouldContain(new string('a', 500));
        message.ShouldNotContain(new string('a', 501));
        message.ShouldEndWith("…");
    }

    [Fact]
    public async Task when_an_error_response_body_never_ends_then_the_failure_is_still_returned()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = new StreamContent(new EndlessStream()) });

        var result = await processor.GetFromJsonAsync<TestPayload>(new Uri("https://example.test/search"), client, CancellationToken.None)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        result.Match(_ => (Exception?)null, ex => ex).ShouldBeOfType<HttpRequestException>();
    }

    [Fact]
    public async Task when_the_limit_falls_inside_a_surrogate_pair_then_the_pair_is_dropped_not_split()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = new StringContent($"{new string('a', 499)}😀tail") });

        var result = await processor.GetFromJsonAsync<TestPayload>(new Uri("https://example.test/search"), client, CancellationToken.None);

        result.Match(_ => string.Empty, ex => ex.Message).ShouldContain($"Response: {new string('a', 499)}…");
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient owns and disposes the handler.")]
    private static HttpClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> responder)
        => new(new StubHttpMessageHandler(responder));

    private sealed record TestPayload(string Name);

    private sealed class EndlessStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Array.Fill(buffer, (byte)'a', offset, count);

            return count;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}
