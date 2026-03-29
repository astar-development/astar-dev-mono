namespace AStar.Dev.Functional.Extensions.Tests.Unit;

public class ResultExtensionsShould
{
    #region Map Tests

    [Fact]
    public void MapSuccessValueWhenResultIsOk()
    {
        var result = new Result<int, string>.Ok(42);

        Result<string, string> mapped = result.Map(value => value.ToString());

        _ = mapped.ShouldBeOfType<Result<string, string>.Ok>();

        string matchResult = mapped.Match(
                                       ok => ok,
                                       err => throw new InvalidOperationException("Should not be error")
                                      );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public void PreserveErrorWhenMappingFailedResult()
    {
        var result = new Result<int, string>.Error("error message");

        Result<string, string> mapped = result.Map(value => value.ToString());

        _ = mapped.ShouldBeOfType<Result<string, string>.Error>();

        string matchResult = mapped.Match(
                                       _ => throw new InvalidOperationException("Should not be success"),
                                       err => err
                                      );

        matchResult.ShouldBe("error message");
    }

    [Fact]
    public async Task MapSuccessValueAsyncWhenResultIsOk()
    {
        var result = new Result<int, string>.Ok(42);

        Result<string, string> mapped = await result.MapAsync(value => Task.FromResult(value.ToString()));

        _ = mapped.ShouldBeOfType<Result<string, string>.Ok>();

        string matchResult = mapped.Match(
                                       ok => ok,
                                       _ => throw new InvalidOperationException("Should not be error")
                                      );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task PreserveErrorWhenMappingFailedResultAsync()
    {
        var result = new Result<int, string>.Error("error message");

        Result<string, string> mapped = await result.MapAsync(value => Task.FromResult(value.ToString()));

        _ = mapped.ShouldBeOfType<Result<string, string>.Error>();

        string matchResult = mapped.Match(
                                       _ => throw new InvalidOperationException("Should not be success"),
                                       err => err
                                      );

        matchResult.ShouldBe("error message");
    }

    [Fact]
    public async Task MapSuccessValueFromTaskResultWhenResultIsOk()
    {
        Task<Result<int, string>> resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));

        Result<string, string> mapped = await resultTask.MapAsync(value => value.ToString());

        _ = mapped.ShouldBeOfType<Result<string, string>.Ok>();

        string matchResult = mapped.Match(
                                       ok => ok,
                                       _ => throw new InvalidOperationException("Should not be error")
                                      );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task PreserveErrorWhenMappingFailedTaskResult()
    {
        Task<Result<int, string>> resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Error("error message"));

        Result<string, string> mapped = await resultTask.MapAsync(value => value.ToString());

        _ = mapped.ShouldBeOfType<Result<string, string>.Error>();

        string matchResult = mapped.Match(
                                       _ => throw new InvalidOperationException("Should not be success"),
                                       err => err
                                      );

        matchResult.ShouldBe("error message");
    }

    [Fact]
    public async Task MapSuccessValueAsyncFromTaskResultWhenResultIsOk()
    {
        Task<Result<int, string>> resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));

        Result<string, string> mapped = await resultTask.MapAsync(value => Task.FromResult(value.ToString()));

        _ = mapped.ShouldBeOfType<Result<string, string>.Ok>();

        string matchResult = mapped.Match(
                                       ok => ok,
                                       _ => throw new InvalidOperationException("Should not be error")
                                      );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task PreserveErrorWhenMappingFailedTaskResultAsync()
    {
        Task<Result<int, string>> resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Error("error message"));

        Result<string, string> mapped = await resultTask.MapAsync(value => Task.FromResult(value.ToString()));

        _ = mapped.ShouldBeOfType<Result<string, string>.Error>();

        string matchResult = mapped.Match(
                                       _ => throw new InvalidOperationException("Should not be success"),
                                       err => err
                                      );

        matchResult.ShouldBe("error message");
    }

    #endregion

    #region MapFailure Tests

    [Fact]
    public void MapErrorValueWhenResultIsError()
    {
        var result = new Result<string, int>.Error(42);

        Result<string, string> mapped = result.MapFailure(error => error.ToString());

        _ = mapped.ShouldBeOfType<Result<string, string>.Error>();

        string matchResult = mapped.Match(
                                       _ => throw new InvalidOperationException("Should not be success"),
                                       err => err
                                      );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public void PreserveSuccessWhenMapFailureOnSuccessResult()
    {
        var result = new Result<string, int>.Ok("success");

        Result<string, string> mapped = result.MapFailure(error => error.ToString());

        _ = mapped.ShouldBeOfType<Result<string, string>.Ok>();

        string matchResult = mapped.Match(
                                       ok => ok,
                                       _ => throw new InvalidOperationException("Should not be error")
                                      );

        matchResult.ShouldBe("success");
    }

    [Fact]
    public async Task MapErrorValueAsyncWhenResultIsError()
    {
        var result = new Result<string, int>.Error(42);

        Result<string, string> mapped = await result.MapFailureAsync(error => Task.FromResult(error.ToString()));

        _ = mapped.ShouldBeOfType<Result<string, string>.Error>();

        string matchResult = mapped.Match(
                                       _ => throw new InvalidOperationException("Should not be success"),
                                       err => err
                                      );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task PreserveSuccessWhenMapFailureAsyncOnSuccessResult()
    {
        var result = new Result<string, int>.Ok("success");

        Result<string, string> mapped = await result.MapFailureAsync(error => Task.FromResult(error.ToString()));

        _ = mapped.ShouldBeOfType<Result<string, string>.Ok>();

        string matchResult = mapped.Match(
                                       ok => ok,
                                       _ => throw new InvalidOperationException("Should not be error")
                                      );

        matchResult.ShouldBe("success");
    }

    [Fact]
    public async Task MapErrorValueFromTaskResultWhenResultIsError()
    {
        Task<Result<string, int>> resultTask = Task.FromResult<Result<string, int>>(new Result<string, int>.Error(42));

        Result<string, string> mapped = await resultTask.MapFailureAsync(error => error.ToString());

        _ = mapped.ShouldBeOfType<Result<string, string>.Error>();

        string matchResult = mapped.Match(
                                       _ => throw new InvalidOperationException("Should not be success"),
                                       err => err
                                      );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task PreserveSuccessWhenMapFailureOnSuccessTaskResult()
    {
        Task<Result<string, int>> resultTask = Task.FromResult<Result<string, int>>(new Result<string, int>.Ok("success"));

        Result<string, string> mapped = await resultTask.MapFailureAsync(error => error.ToString());

        _ = mapped.ShouldBeOfType<Result<string, string>.Ok>();

        string matchResult = mapped.Match(
                                       ok => ok,
                                       _ => throw new InvalidOperationException("Should not be error")
                                      );

        matchResult.ShouldBe("success");
    }

    [Fact]
    public async Task MapErrorValueAsyncFromTaskResultWhenResultIsError()
    {
        Task<Result<string, int>> resultTask = Task.FromResult<Result<string, int>>(new Result<string, int>.Error(42));

        Result<string, string> mapped = await resultTask.MapFailureAsync(error => Task.FromResult(error.ToString()));

        _ = mapped.ShouldBeOfType<Result<string, string>.Error>();

        string matchResult = mapped.Match(
                                       _ => throw new InvalidOperationException("Should not be success"),
                                       err => err
                                      );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task PreserveSuccessWhenMapFailureAsyncOnSuccessTaskResult()
    {
        Task<Result<string, int>> resultTask = Task.FromResult<Result<string, int>>(new Result<string, int>.Ok("success"));

        Result<string, string> mapped = await resultTask.MapFailureAsync(error => Task.FromResult(error.ToString()));

        _ = mapped.ShouldBeOfType<Result<string, string>.Ok>();

        string matchResult = mapped.Match(
                                       ok => ok,
                                       _ => throw new InvalidOperationException("Should not be error")
                                      );

        matchResult.ShouldBe("success");
    }

    #endregion

    #region Bind Tests

    [Fact]
    public void BindSuccessValueToNewResultWhenResultIsOk()
    {
        var result = new Result<int, string>.Ok(42);

        Result<string, string> bound = result.Bind(value => new Result<string, string>.Ok(value.ToString()));

        _ = bound.ShouldBeOfType<Result<string, string>.Ok>();

        string matchResult = bound.Match(
                                      ok => ok,
                                      _ => throw new InvalidOperationException("Should not be error")
                                     );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public void BindSuccessValueToErrorResultWhenBindFunctionReturnsError()
    {
        var result = new Result<int, string>.Ok(42);

        Result<string, string> bound = result.Bind<int, string, string>(value => new Result<string, string>.Error("bound error"));

        _ = bound.ShouldBeOfType<Result<string, string>.Error>();

        string matchResult = bound.Match(
                                      _ => throw new InvalidOperationException("Should not be success"),
                                      err => err
                                     );

        matchResult.ShouldBe("bound error");
    }

    [Fact]
    public void PreserveErrorWhenBindingFailedResult()
    {
        var result = new Result<int, string>.Error("original error");

        Result<string, string> bound = result.Bind<int, string, string>(value => new Result<string, string>.Ok(value.ToString()));

        _ = bound.ShouldBeOfType<Result<string, string>.Error>();

        string matchResult = bound.Match(
                                      _ => throw new InvalidOperationException("Should not be success"),
                                      err => err
                                     );

        matchResult.ShouldBe("original error");
    }

    [Fact]
    public async Task BindSuccessValueAsyncToNewResultWhenResultIsOk()
    {
        var result = new Result<int, string>.Ok(42);

        Result<string, string> bound = await result.BindAsync(value => Task.FromResult<Result<string, string>>(new Result<string, string>.Ok(value.ToString())));

        _ = bound.ShouldBeOfType<Result<string, string>.Ok>();

        string matchResult = bound.Match(
                                      ok => ok,
                                      _ => throw new InvalidOperationException("Should not be error")
                                     );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task BindSuccessValueAsyncToErrorResultWhenBindFunctionReturnsError()
    {
        var result = new Result<int, string>.Ok(42);

        Result<string, string> bound = await result.BindAsync(value => Task.FromResult<Result<string, string>>(new Result<string, string>.Error("bound error")));

        _ = bound.ShouldBeOfType<Result<string, string>.Error>();

        string matchResult = bound.Match(
                                      _ => throw new InvalidOperationException("Should not be success"),
                                      err => err
                                     );

        matchResult.ShouldBe("bound error");
    }

    [Fact]
    public async Task PreserveErrorWhenBindingAsyncFailedResult()
    {
        var result = new Result<int, string>.Error("original error");

        Result<string, string> bound = await result.BindAsync(value => Task.FromResult<Result<string, string>>(new Result<string, string>.Ok(value.ToString())));

        _ = bound.ShouldBeOfType<Result<string, string>.Error>();

        string matchResult = bound.Match(
                                      _ => throw new InvalidOperationException("Should not be success"),
                                      err => err
                                     );

        matchResult.ShouldBe("original error");
    }

    [Fact]
    public async Task BindSuccessValueFromTaskResultWhenResultIsOk()
    {
        Task<Result<int, string>> resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));

        Result<string, string> bound = await resultTask.BindAsync(value => new Result<string, string>.Ok(value.ToString()));

        _ = bound.ShouldBeOfType<Result<string, string>.Ok>();

        string matchResult = bound.Match(
                                      ok => ok,
                                      _ => throw new InvalidOperationException("Should not be error")
                                     );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task BindSuccessValueFromTaskResultToErrorWhenBindFunctionReturnsError()
    {
        Task<Result<int, string>> resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));

        Result<string, string> bound = await resultTask.BindAsync(value => new Result<string, string>.Error("bound error"));

        _ = bound.ShouldBeOfType<Result<string, string>.Error>();

        string matchResult = bound.Match(
                                      _ => throw new InvalidOperationException("Should not be success"),
                                      err => err
                                     );

        matchResult.ShouldBe("bound error");
    }

    [Fact]
    public async Task PreserveErrorWhenBindingFailedTaskResult()
    {
        Task<Result<int, string>> resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Error("original error"));

        Result<string, string> bound = await resultTask.BindAsync(value => new Result<string, string>.Ok(value.ToString()));

        _ = bound.ShouldBeOfType<Result<string, string>.Error>();

        string matchResult = bound.Match(
                                      _ => throw new InvalidOperationException("Should not be success"),
                                      err => err
                                     );

        matchResult.ShouldBe("original error");
    }

    [Fact]
    public async Task BindSuccessValueFromTaskResultAsyncWhenResultIsOk()
    {
        Task<Result<int, string>> resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));

        Result<string, string> bound = await resultTask.BindAsync(value => Task.FromResult<Result<string, string>>(new Result<string, string>.Ok(value.ToString())));

        _ = bound.ShouldBeOfType<Result<string, string>.Ok>();

        string matchResult = bound.Match(
                                      ok => ok,
                                      _ => throw new InvalidOperationException("Should not be error")
                                     );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task BindSuccessValueFromTaskResultAsyncToErrorWhenBindFunctionReturnsError()
    {
        Task<Result<int, string>> resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));

        Result<string, string> bound = await resultTask.BindAsync(value => Task.FromResult<Result<string, string>>(new Result<string, string>.Error("bound error")));

        _ = bound.ShouldBeOfType<Result<string, string>.Error>();

        string matchResult = bound.Match(
                                      _ => throw new InvalidOperationException("Should not be success"),
                                      err => err
                                     );

        matchResult.ShouldBe("bound error");
    }

    [Fact]
    public async Task PreserveErrorWhenBindingFailedTaskResultAsync()
    {
        Task<Result<int, string>> resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Error("original error"));

        Result<string, string> bound = await resultTask.BindAsync(value => Task.FromResult<Result<string, string>>(new Result<string, string>.Ok(value.ToString())));

        _ = bound.ShouldBeOfType<Result<string, string>.Error>();

        string matchResult = bound.Match(
                                      _ => throw new InvalidOperationException("Should not be success"),
                                      err => err
                                     );

        matchResult.ShouldBe("original error");
    }

    #endregion

    #region Tap Tests

    [Fact]
    public void ExecuteSideEffectAndReturnOriginalResultWhenTappingSuccessResult()
    {
        var result          = new Result<int, string>.Ok(42);
        int sideEffectValue = 0;

        Result<int, string> tapped = result.Tap(value => sideEffectValue = value);

        sideEffectValue.ShouldBe(42);
        tapped.ShouldBeSameAs(result);
    }

    [Fact]
    public void NotExecuteSideEffectAndReturnOriginalResultWhenTappingErrorResult()
    {
        var result          = new Result<int, string>.Error("error");
        int sideEffectValue = 0;

        Result<int, string> tapped = result.Tap(value => sideEffectValue = value);

        sideEffectValue.ShouldBe(0);
        tapped.ShouldBeSameAs(result);
    }

    [Fact]
    public void ExecuteSideEffectAndReturnOriginalResultWhenTappingErrorOnErrorResult()
    {
        var result          = new Result<string, int>.Error(42);
        int sideEffectValue = 0;

        Result<string, int> tapped = result.TapError(value => sideEffectValue = value);

        sideEffectValue.ShouldBe(42);
        tapped.ShouldBeSameAs(result);
    }

    [Fact]
    public void NotExecuteSideEffectAndReturnOriginalResultWhenTappingErrorOnSuccessResult()
    {
        var result          = new Result<string, int>.Ok("success");
        int sideEffectValue = 0;

        Result<string, int> tapped = result.TapError(value => sideEffectValue = value);

        sideEffectValue.ShouldBe(0);
        tapped.ShouldBeSameAs(result);
    }

    [Fact]
    public async Task ExecuteAsyncSideEffectAndReturnOriginalResultWhenTappingSuccessResult()
    {
        var result          = new Result<int, string>.Ok(42);
        int sideEffectValue = 0;

        Result<int, string> tapped = await result.TapAsync(value =>
                                           {
                                               sideEffectValue = value;

                                               return Task.CompletedTask;
                                           });

        sideEffectValue.ShouldBe(42);
        tapped.ShouldBeSameAs(result);
    }

    [Fact]
    public async Task NotExecuteAsyncSideEffectAndReturnOriginalResultWhenTappingErrorResult()
    {
        var result          = new Result<int, string>.Error("error");
        int sideEffectValue = 0;

        Result<int, string> tapped = await result.TapAsync(value =>
                                           {
                                               sideEffectValue = value;

                                               return Task.CompletedTask;
                                           });

        sideEffectValue.ShouldBe(0);
        tapped.ShouldBeSameAs(result);
    }

    [Fact]
    public async Task ExecuteAsyncSideEffectAndReturnOriginalResultWhenTappingErrorOnErrorResult()
    {
        var result          = new Result<string, int>.Error(42);
        int sideEffectValue = 0;

        Result<string, int> tapped = await result.TapErrorAsync(value =>
                                                {
                                                    sideEffectValue = value;

                                                    return Task.CompletedTask;
                                                });

        sideEffectValue.ShouldBe(42);
        tapped.ShouldBeSameAs(result);
    }

    [Fact]
    public async Task NotExecuteAsyncSideEffectAndReturnOriginalResultWhenTappingErrorOnSuccessResult()
    {
        var result          = new Result<string, int>.Ok("success");
        int sideEffectValue = 0;

        Result<string, int> tapped = await result.TapErrorAsync(value =>
                                                {
                                                    sideEffectValue = value;

                                                    return Task.CompletedTask;
                                                });

        sideEffectValue.ShouldBe(0);
        tapped.ShouldBeSameAs(result);
    }

    [Fact]
    public async Task ExecuteSideEffectAndReturnOriginalResultWhenTappingSuccessTaskResult()
    {
        Task<Result<int, string>> resultTask      = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));
        int sideEffectValue = 0;

        Result<int, string> tapped = await resultTask.TapAsync(value => sideEffectValue = value);

        sideEffectValue.ShouldBe(42);
        tapped.ShouldBe(await resultTask);
    }

    [Fact]
    public async Task NotExecuteSideEffectAndReturnOriginalResultWhenTappingErrorTaskResult()
    {
        Task<Result<int, string>> resultTask      = Task.FromResult<Result<int, string>>(new Result<int, string>.Error("error"));
        int sideEffectValue = 0;

        Result<int, string> tapped = await resultTask.TapAsync(value => sideEffectValue = value);

        sideEffectValue.ShouldBe(0);
        tapped.ShouldBe(await resultTask);
    }

    [Fact]
    public async Task ExecuteSideEffectAndReturnOriginalResultWhenTappingErrorOnErrorTaskResult()
    {
        Task<Result<string, int>> resultTask      = Task.FromResult<Result<string, int>>(new Result<string, int>.Error(42));
        int sideEffectValue = 0;

        Result<string, int> tapped = await resultTask.TapErrorAsync(value => sideEffectValue = value);

        sideEffectValue.ShouldBe(42);
        tapped.ShouldBe(await resultTask);
    }

    [Fact]
    public async Task NotExecuteSideEffectAndReturnOriginalResultWhenTappingErrorOnSuccessTaskResult()
    {
        Task<Result<string, int>> resultTask      = Task.FromResult<Result<string, int>>(new Result<string, int>.Ok("success"));
        int sideEffectValue = 0;

        Result<string, int> tapped = await resultTask.TapErrorAsync(value => sideEffectValue = value);

        sideEffectValue.ShouldBe(0);
        tapped.ShouldBe(await resultTask);
    }

    [Fact]
    public async Task ExecuteAsyncSideEffectAndReturnOriginalResultWhenTappingSuccessTaskResult()
    {
        Task<Result<int, string>> resultTask      = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));
        int sideEffectValue = 0;

        Result<int, string> tapped = await resultTask.TapAsync(value =>
                                               {
                                                   sideEffectValue = value;

                                                   return Task.CompletedTask;
                                               });

        sideEffectValue.ShouldBe(42);
        tapped.ShouldBe(await resultTask);
    }

    [Fact]
    public async Task NotExecuteAsyncSideEffectAndReturnOriginalResultWhenTappingErrorTaskResult()
    {
        Task<Result<int, string>> resultTask      = Task.FromResult<Result<int, string>>(new Result<int, string>.Error("error"));
        int sideEffectValue = 0;

        Result<int, string> tapped = await resultTask.TapAsync(value =>
                                               {
                                                   sideEffectValue = value;

                                                   return Task.CompletedTask;
                                               });

        sideEffectValue.ShouldBe(0);
        tapped.ShouldBe(await resultTask);
    }

    [Fact]
    public async Task ExecuteAsyncSideEffectAndReturnOriginalResultWhenTappingErrorOnErrorTaskResult()
    {
        Task<Result<string, int>> resultTask      = Task.FromResult<Result<string, int>>(new Result<string, int>.Error(42));
        int sideEffectValue = 0;

        Result<string, int> tapped = await resultTask.TapErrorAsync(value =>
                                                    {
                                                        sideEffectValue = value;

                                                        return Task.CompletedTask;
                                                    });

        sideEffectValue.ShouldBe(42);
        tapped.ShouldBe(await resultTask);
    }

    [Fact]
    public async Task NotExecuteAsyncSideEffectAndReturnOriginalResultWhenTappingErrorOnSuccessTaskResult()
    {
        Task<Result<string, int>> resultTask      = Task.FromResult<Result<string, int>>(new Result<string, int>.Ok("success"));
        int sideEffectValue = 0;

        Result<string, int> tapped = await resultTask.TapErrorAsync(value =>
                                                    {
                                                        sideEffectValue = value;

                                                        return Task.CompletedTask;
                                                    });

        sideEffectValue.ShouldBe(0);
        tapped.ShouldBe(await resultTask);
    }

    #endregion
}
