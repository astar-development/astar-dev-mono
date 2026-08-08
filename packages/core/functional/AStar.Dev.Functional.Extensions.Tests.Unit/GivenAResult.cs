namespace AStar.Dev.Functional.Extensions.Tests.Unit;

public sealed class GivenAResult
{
    [Fact]
    public void when_result_is_ok_then_match_invokes_the_success_handler()
    {
        var result = new Result<string, int>.Ok("success");

        string matched = result.Match(
                                   success => $"Success: {success}",
                                   error => $"Error: {error}");

        matched.ShouldBe("Success: success");
    }

    [Fact]
    public void when_result_is_error_then_match_invokes_the_error_handler()
    {
        var result = new Result<string, int>.Error(42);

        string matched = result.Match(
                                   success => $"Success: {success}",
                                   error => $"Error: {error}");

        matched.ShouldBe("Error: 42");
    }

    [Fact]
    public async Task when_result_is_ok_then_match_async_invokes_the_async_success_handler()
    {
        var result = new Result<string, int>.Ok("success");

        string matched = await result.MatchAsync(
                                              success => Task.FromResult($"Success: {success}"),
                                              error => $"Error: {error}");

        matched.ShouldBe("Success: success");
    }

    [Fact]
    public async Task when_result_is_error_then_match_async_invokes_the_error_handler()
    {
        var result = new Result<string, int>.Error(42);

        string matched = await result.MatchAsync(
                                              success => Task.FromResult($"Success: {success}"),
                                              error => $"Error: {error}");

        matched.ShouldBe("Error: 42");
    }

    [Fact]
    public async Task when_result_is_ok_then_match_async_with_sync_success_and_async_error_handler_invokes_the_success_handler()
    {
        var result = new Result<string, int>.Ok("success");

        string matched = await result.MatchAsync(
                                              success => $"Success: {success}",
                                              error => Task.FromResult($"Error: {error}"));

        matched.ShouldBe("Success: success");
    }

    [Fact]
    public async Task when_result_is_error_then_match_async_with_sync_success_and_async_error_handler_invokes_the_error_handler()
    {
        var result = new Result<string, int>.Error(42);

        string matched = await result.MatchAsync(
                                              success => $"Success: {success}",
                                              error => Task.FromResult($"Error: {error}"));

        matched.ShouldBe("Error: 42");
    }

    [Fact]
    public async Task when_result_is_ok_then_match_async_with_async_success_and_async_error_handler_invokes_the_success_handler()
    {
        var result = new Result<string, int>.Ok("success");

        string matched = await result.MatchAsync(
                                              success => Task.FromResult($"Success: {success}"),
                                              error => Task.FromResult($"Error: {error}"));

        matched.ShouldBe("Success: success");
    }

    [Fact]
    public async Task when_result_is_error_then_match_async_with_async_success_and_async_error_handler_invokes_the_error_handler()
    {
        var result = new Result<string, int>.Error(42);

        string matched = await result.MatchAsync(
                                              success => Task.FromResult($"Success: {success}"),
                                              error => Task.FromResult($"Error: {error}"));

        matched.ShouldBe("Error: 42");
    }

    [Fact]
    public void when_an_ok_result_is_created_then_it_holds_the_expected_value()
    {
        string value = "test value";

        var result = new Result<string, int>.Ok(value);

        result.Value.ShouldBe(value);
    }

    [Fact]
    public void when_an_error_result_is_created_then_it_holds_the_expected_reason()
    {
        int reason = 42;

        var result = new Result<string, int>.Error(reason);

        result.Reason.ShouldBe(reason);
    }
}
