namespace AStar.Dev.Functional.Extensions.Tests.Unit;

public class ResultExtensionsShould
{
    #region Map Tests

    [Fact]
    public void MapSuccessValueWhenResultIsOk()
    {
        var result = new Result<int, string>.Ok(42);

        var mapped = result.Map(value => value.ToString());

        mapped.ShouldBeOfType<Result<string, string>.Ok>();

        var matchResult = mapped.Match(
                                       ok => ok,
                                       err => throw new InvalidOperationException("Should not be error")
                                      );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public void PreserveErrorWhenMappingFailedResult()
    {
        var result = new Result<int, string>.Error("error message");

        var mapped = result.Map(value => value.ToString());

        mapped.ShouldBeOfType<Result<string, string>.Error>();

        var matchResult = mapped.Match(
                                       _ => throw new InvalidOperationException("Should not be success"),
                                       err => err
                                      );

        matchResult.ShouldBe("error message");
    }

    [Fact]
    public async Task MapSuccessValueAsyncWhenResultIsOk()
    {
        var result = new Result<int, string>.Ok(42);

        var mapped = await result.MapAsync(value => Task.FromResult(value.ToString()));

        mapped.ShouldBeOfType<Result<string, string>.Ok>();

        var matchResult = mapped.Match(
                                       ok => ok,
                                       _ => throw new InvalidOperationException("Should not be error")
                                      );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task PreserveErrorWhenMappingFailedResultAsync()
    {
        var result = new Result<int, string>.Error("error message");

        var mapped = await result.MapAsync(value => Task.FromResult(value.ToString()));

        mapped.ShouldBeOfType<Result<string, string>.Error>();

        var matchResult = mapped.Match(
                                       _ => throw new InvalidOperationException("Should not be success"),
                                       err => err
                                      );

        matchResult.ShouldBe("error message");
    }

    [Fact]
    public async Task MapSuccessValueFromTaskResultWhenResultIsOk()
    {
        var resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));

        var mapped = await resultTask.MapAsync(value => value.ToString());

        mapped.ShouldBeOfType<Result<string, string>.Ok>();

        var matchResult = mapped.Match(
                                       ok => ok,
                                       _ => throw new InvalidOperationException("Should not be error")
                                      );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task PreserveErrorWhenMappingFailedTaskResult()
    {
        var resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Error("error message"));

        var mapped = await resultTask.MapAsync(value => value.ToString());

        mapped.ShouldBeOfType<Result<string, string>.Error>();

        var matchResult = mapped.Match(
                                       _ => throw new InvalidOperationException("Should not be success"),
                                       err => err
                                      );

        matchResult.ShouldBe("error message");
    }

    [Fact]
    public async Task MapSuccessValueAsyncFromTaskResultWhenResultIsOk()
    {
        var resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));

        var mapped = await resultTask.MapAsync(value => Task.FromResult(value.ToString()));

        mapped.ShouldBeOfType<Result<string, string>.Ok>();

        var matchResult = mapped.Match(
                                       ok => ok,
                                       _ => throw new InvalidOperationException("Should not be error")
                                      );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task PreserveErrorWhenMappingFailedTaskResultAsync()
    {
        var resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Error("error message"));

        var mapped = await resultTask.MapAsync(value => Task.FromResult(value.ToString()));

        mapped.ShouldBeOfType<Result<string, string>.Error>();

        var matchResult = mapped.Match(
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

        var mapped = result.MapFailure(error => error.ToString());

        mapped.ShouldBeOfType<Result<string, string>.Error>();

        var matchResult = mapped.Match(
                                       _ => throw new InvalidOperationException("Should not be success"),
                                       err => err
                                      );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public void PreserveSuccessWhenMapFailureOnSuccessResult()
    {
        var result = new Result<string, int>.Ok("success");

        var mapped = result.MapFailure(error => error.ToString());

        mapped.ShouldBeOfType<Result<string, string>.Ok>();

        var matchResult = mapped.Match(
                                       ok => ok,
                                       _ => throw new InvalidOperationException("Should not be error")
                                      );

        matchResult.ShouldBe("success");
    }

    [Fact]
    public async Task MapErrorValueAsyncWhenResultIsError()
    {
        var result = new Result<string, int>.Error(42);

        var mapped = await result.MapFailureAsync(error => Task.FromResult(error.ToString()));

        mapped.ShouldBeOfType<Result<string, string>.Error>();

        var matchResult = mapped.Match(
                                       _ => throw new InvalidOperationException("Should not be success"),
                                       err => err
                                      );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task PreserveSuccessWhenMapFailureAsyncOnSuccessResult()
    {
        var result = new Result<string, int>.Ok("success");

        var mapped = await result.MapFailureAsync(error => Task.FromResult(error.ToString()));

        mapped.ShouldBeOfType<Result<string, string>.Ok>();

        var matchResult = mapped.Match(
                                       ok => ok,
                                       _ => throw new InvalidOperationException("Should not be error")
                                      );

        matchResult.ShouldBe("success");
    }

    [Fact]
    public async Task MapErrorValueFromTaskResultWhenResultIsError()
    {
        var resultTask = Task.FromResult<Result<string, int>>(new Result<string, int>.Error(42));

        var mapped = await resultTask.MapFailureAsync(error => error.ToString());

        mapped.ShouldBeOfType<Result<string, string>.Error>();

        var matchResult = mapped.Match(
                                       _ => throw new InvalidOperationException("Should not be success"),
                                       err => err
                                      );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task PreserveSuccessWhenMapFailureOnSuccessTaskResult()
    {
        var resultTask = Task.FromResult<Result<string, int>>(new Result<string, int>.Ok("success"));

        var mapped = await resultTask.MapFailureAsync(error => error.ToString());

        mapped.ShouldBeOfType<Result<string, string>.Ok>();

        var matchResult = mapped.Match(
                                       ok => ok,
                                       _ => throw new InvalidOperationException("Should not be error")
                                      );

        matchResult.ShouldBe("success");
    }

    [Fact]
    public async Task MapErrorValueAsyncFromTaskResultWhenResultIsError()
    {
        var resultTask = Task.FromResult<Result<string, int>>(new Result<string, int>.Error(42));

        var mapped = await resultTask.MapFailureAsync(error => Task.FromResult(error.ToString()));

        mapped.ShouldBeOfType<Result<string, string>.Error>();

        var matchResult = mapped.Match(
                                       _ => throw new InvalidOperationException("Should not be success"),
                                       err => err
                                      );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task PreserveSuccessWhenMapFailureAsyncOnSuccessTaskResult()
    {
        var resultTask = Task.FromResult<Result<string, int>>(new Result<string, int>.Ok("success"));

        var mapped = await resultTask.MapFailureAsync(error => Task.FromResult(error.ToString()));

        mapped.ShouldBeOfType<Result<string, string>.Ok>();

        var matchResult = mapped.Match(
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

        var bound = result.Bind(value => new Result<string, string>.Ok(value.ToString()));

        bound.ShouldBeOfType<Result<string, string>.Ok>();

        var matchResult = bound.Match(
                                      ok => ok,
                                      _ => throw new InvalidOperationException("Should not be error")
                                     );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public void BindSuccessValueToErrorResultWhenBindFunctionReturnsError()
    {
        var result = new Result<int, string>.Ok(42);

        var bound = result.Bind<int, string, string>(value => new Result<string, string>.Error("bound error"));

        bound.ShouldBeOfType<Result<string, string>.Error>();

        var matchResult = bound.Match(
                                      _ => throw new InvalidOperationException("Should not be success"),
                                      err => err
                                     );

        matchResult.ShouldBe("bound error");
    }

    [Fact]
    public void PreserveErrorWhenBindingFailedResult()
    {
        var result = new Result<int, string>.Error("original error");

        var bound = result.Bind<int, string, string>(value => new Result<string, string>.Ok(value.ToString()));

        bound.ShouldBeOfType<Result<string, string>.Error>();

        var matchResult = bound.Match(
                                      _ => throw new InvalidOperationException("Should not be success"),
                                      err => err
                                     );

        matchResult.ShouldBe("original error");
    }

    [Fact]
    public async Task BindSuccessValueAsyncToNewResultWhenResultIsOk()
    {
        var result = new Result<int, string>.Ok(42);

        var bound = await result.BindAsync(value => Task.FromResult<Result<string, string>>(new Result<string, string>.Ok(value.ToString())));

        bound.ShouldBeOfType<Result<string, string>.Ok>();

        var matchResult = bound.Match(
                                      ok => ok,
                                      _ => throw new InvalidOperationException("Should not be error")
                                     );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task BindSuccessValueAsyncToErrorResultWhenBindFunctionReturnsError()
    {
        var result = new Result<int, string>.Ok(42);

        var bound = await result.BindAsync(value => Task.FromResult<Result<string, string>>(new Result<string, string>.Error("bound error")));

        bound.ShouldBeOfType<Result<string, string>.Error>();

        var matchResult = bound.Match(
                                      _ => throw new InvalidOperationException("Should not be success"),
                                      err => err
                                     );

        matchResult.ShouldBe("bound error");
    }

    [Fact]
    public async Task PreserveErrorWhenBindingAsyncFailedResult()
    {
        var result = new Result<int, string>.Error("original error");

        var bound = await result.BindAsync(value => Task.FromResult<Result<string, string>>(new Result<string, string>.Ok(value.ToString())));

        bound.ShouldBeOfType<Result<string, string>.Error>();

        var matchResult = bound.Match(
                                      _ => throw new InvalidOperationException("Should not be success"),
                                      err => err
                                     );

        matchResult.ShouldBe("original error");
    }

    [Fact]
    public async Task BindSuccessValueFromTaskResultWhenResultIsOk()
    {
        var resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));

        var bound = await resultTask.BindAsync(value => new Result<string, string>.Ok(value.ToString()));

        bound.ShouldBeOfType<Result<string, string>.Ok>();

        var matchResult = bound.Match(
                                      ok => ok,
                                      _ => throw new InvalidOperationException("Should not be error")
                                     );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task BindSuccessValueFromTaskResultToErrorWhenBindFunctionReturnsError()
    {
        var resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));

        var bound = await resultTask.BindAsync(value => new Result<string, string>.Error("bound error"));

        bound.ShouldBeOfType<Result<string, string>.Error>();

        var matchResult = bound.Match(
                                      _ => throw new InvalidOperationException("Should not be success"),
                                      err => err
                                     );

        matchResult.ShouldBe("bound error");
    }

    [Fact]
    public async Task PreserveErrorWhenBindingFailedTaskResult()
    {
        var resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Error("original error"));

        var bound = await resultTask.BindAsync(value => new Result<string, string>.Ok(value.ToString()));

        bound.ShouldBeOfType<Result<string, string>.Error>();

        var matchResult = bound.Match(
                                      _ => throw new InvalidOperationException("Should not be success"),
                                      err => err
                                     );

        matchResult.ShouldBe("original error");
    }

    [Fact]
    public async Task BindSuccessValueFromTaskResultAsyncWhenResultIsOk()
    {
        var resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));

        var bound = await resultTask.BindAsync(value => Task.FromResult<Result<string, string>>(new Result<string, string>.Ok(value.ToString())));

        bound.ShouldBeOfType<Result<string, string>.Ok>();

        var matchResult = bound.Match(
                                      ok => ok,
                                      _ => throw new InvalidOperationException("Should not be error")
                                     );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task BindSuccessValueFromTaskResultAsyncToErrorWhenBindFunctionReturnsError()
    {
        var resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));

        var bound = await resultTask.BindAsync(value => Task.FromResult<Result<string, string>>(new Result<string, string>.Error("bound error")));

        bound.ShouldBeOfType<Result<string, string>.Error>();

        var matchResult = bound.Match(
                                      _ => throw new InvalidOperationException("Should not be success"),
                                      err => err
                                     );

        matchResult.ShouldBe("bound error");
    }

    [Fact]
    public async Task PreserveErrorWhenBindingFailedTaskResultAsync()
    {
        var resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Error("original error"));

        var bound = await resultTask.BindAsync(value => Task.FromResult<Result<string, string>>(new Result<string, string>.Ok(value.ToString())));

        bound.ShouldBeOfType<Result<string, string>.Error>();

        var matchResult = bound.Match(
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
        var sideEffectValue = 0;

        var tapped = result.Tap(value => sideEffectValue = value);

        sideEffectValue.ShouldBe(42);
        tapped.ShouldBeSameAs(result);
    }

    [Fact]
    public void NotExecuteSideEffectAndReturnOriginalResultWhenTappingErrorResult()
    {
        var result          = new Result<int, string>.Error("error");
        var sideEffectValue = 0;

        var tapped = result.Tap(value => sideEffectValue = value);

        sideEffectValue.ShouldBe(0);
        tapped.ShouldBeSameAs(result);
    }

    [Fact]
    public void ExecuteSideEffectAndReturnOriginalResultWhenTappingErrorOnErrorResult()
    {
        var result          = new Result<string, int>.Error(42);
        var sideEffectValue = 0;

        var tapped = result.TapError(value => sideEffectValue = value);

        sideEffectValue.ShouldBe(42);
        tapped.ShouldBeSameAs(result);
    }

    [Fact]
    public void NotExecuteSideEffectAndReturnOriginalResultWhenTappingErrorOnSuccessResult()
    {
        var result          = new Result<string, int>.Ok("success");
        var sideEffectValue = 0;

        var tapped = result.TapError(value => sideEffectValue = value);

        sideEffectValue.ShouldBe(0);
        tapped.ShouldBeSameAs(result);
    }

    [Fact]
    public async Task ExecuteAsyncSideEffectAndReturnOriginalResultWhenTappingSuccessResult()
    {
        var result          = new Result<int, string>.Ok(42);
        var sideEffectValue = 0;

        var tapped = await result.TapAsync(value =>
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
        var sideEffectValue = 0;

        var tapped = await result.TapAsync(value =>
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
        var sideEffectValue = 0;

        var tapped = await result.TapErrorAsync(value =>
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
        var sideEffectValue = 0;

        var tapped = await result.TapErrorAsync(value =>
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
        var resultTask      = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));
        var sideEffectValue = 0;

        var tapped = await resultTask.TapAsync(value => sideEffectValue = value);

        sideEffectValue.ShouldBe(42);
        tapped.ShouldBe(await resultTask);
    }

    [Fact]
    public async Task NotExecuteSideEffectAndReturnOriginalResultWhenTappingErrorTaskResult()
    {
        var resultTask      = Task.FromResult<Result<int, string>>(new Result<int, string>.Error("error"));
        var sideEffectValue = 0;

        var tapped = await resultTask.TapAsync(value => sideEffectValue = value);

        sideEffectValue.ShouldBe(0);
        tapped.ShouldBe(await resultTask);
    }

    [Fact]
    public async Task ExecuteSideEffectAndReturnOriginalResultWhenTappingErrorOnErrorTaskResult()
    {
        var resultTask      = Task.FromResult<Result<string, int>>(new Result<string, int>.Error(42));
        var sideEffectValue = 0;

        var tapped = await resultTask.TapErrorAsync(value => sideEffectValue = value);

        sideEffectValue.ShouldBe(42);
        tapped.ShouldBe(await resultTask);
    }

    [Fact]
    public async Task NotExecuteSideEffectAndReturnOriginalResultWhenTappingErrorOnSuccessTaskResult()
    {
        var resultTask      = Task.FromResult<Result<string, int>>(new Result<string, int>.Ok("success"));
        var sideEffectValue = 0;

        var tapped = await resultTask.TapErrorAsync(value => sideEffectValue = value);

        sideEffectValue.ShouldBe(0);
        tapped.ShouldBe(await resultTask);
    }

    [Fact]
    public async Task ExecuteAsyncSideEffectAndReturnOriginalResultWhenTappingSuccessTaskResult()
    {
        var resultTask      = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));
        var sideEffectValue = 0;

        var tapped = await resultTask.TapAsync(value =>
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
        var resultTask      = Task.FromResult<Result<int, string>>(new Result<int, string>.Error("error"));
        var sideEffectValue = 0;

        var tapped = await resultTask.TapAsync(value =>
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
        var resultTask      = Task.FromResult<Result<string, int>>(new Result<string, int>.Error(42));
        var sideEffectValue = 0;

        var tapped = await resultTask.TapErrorAsync(value =>
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
        var resultTask      = Task.FromResult<Result<string, int>>(new Result<string, int>.Ok("success"));
        var sideEffectValue = 0;

        var tapped = await resultTask.TapErrorAsync(value =>
                                                    {
                                                        sideEffectValue = value;

                                                        return Task.CompletedTask;
                                                    });

        sideEffectValue.ShouldBe(0);
        tapped.ShouldBe(await resultTask);
    }

    #endregion
}
