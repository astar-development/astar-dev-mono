namespace AStar.Dev.Functional.Extensions.Tests.Unit;

public class ResultShould
{
    [Fact]
    public void MatchToSuccessHandlerWhenResultIsOk()
    {
        var result = new Result<string, int>.Ok("success");

        var matched = result.Match(
                                   success => $"Success: {success}",
                                   error => $"Error: {error}");

        matched.ShouldBe("Success: success");
    }

    [Fact]
    public void MatchToErrorHandlerWhenResultIsError()
    {
        var result = new Result<string, int>.Error(42);

        var matched = result.Match(
                                   success => $"Success: {success}",
                                   error => $"Error: {error}");

        matched.ShouldBe("Error: 42");
    }

    [Fact]
    public async Task MatchAsyncToSuccessAsyncHandlerWhenResultIsOk()
    {
        var result = new Result<string, int>.Ok("success");

        var matched = await result.MatchAsync(
                                              success => Task.FromResult($"Success: {success}"),
                                              error => $"Error: {error}");

        matched.ShouldBe("Success: success");
    }

    [Fact]
    public async Task MatchAsyncToErrorHandlerWhenResultIsError()
    {
        var result = new Result<string, int>.Error(42);

        var matched = await result.MatchAsync(
                                              success => Task.FromResult($"Success: {success}"),
                                              error => $"Error: {error}");

        matched.ShouldBe("Error: 42");
    }

    [Fact]
    public async Task MatchAsyncToSuccessHandlerAndAsyncErrorHandlerWhenResultIsOk()
    {
        var result = new Result<string, int>.Ok("success");

        var matched = await result.MatchAsync(
                                              success => $"Success: {success}",
                                              error => Task.FromResult($"Error: {error}"));

        matched.ShouldBe("Success: success");
    }

    [Fact]
    public async Task MatchAsyncToSuccessHandlerAndAsyncErrorHandlerWhenResultIsError()
    {
        var result = new Result<string, int>.Error(42);

        var matched = await result.MatchAsync(
                                              success => $"Success: {success}",
                                              error => Task.FromResult($"Error: {error}"));

        matched.ShouldBe("Error: 42");
    }

    [Fact]
    public async Task MatchAsyncToAsyncSuccessHandlerAndAsyncErrorHandlerWhenResultIsOk()
    {
        var result = new Result<string, int>.Ok("success");

        var matched = await result.MatchAsync(
                                              success => Task.FromResult($"Success: {success}"),
                                              error => Task.FromResult($"Error: {error}"));

        matched.ShouldBe("Success: success");
    }

    [Fact]
    public async Task MatchAsyncToAsyncSuccessHandlerAndAsyncErrorHandlerWhenResultIsError()
    {
        var result = new Result<string, int>.Error(42);

        var matched = await result.MatchAsync(
                                              success => Task.FromResult($"Success: {success}"),
                                              error => Task.FromResult($"Error: {error}"));

        matched.ShouldBe("Error: 42");
    }

    [Fact]
    public void CreateOkResultWithCorrectValue()
    {
        var value = "test value";

        var result = new Result<string, int>.Ok(value);

        result.Value.ShouldBe(value);
    }

    [Fact]
    public void CreateErrorResultWithCorrectReason()
    {
        var reason = 42;

        var result = new Result<string, int>.Error(reason);

        result.Reason.ShouldBe(reason);
    }
}
