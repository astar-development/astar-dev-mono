# AStarDev.FunctionalParadigm

Functional programming building blocks for .NET: explicit success/failure, optional values and validation, with no `null` returns and no exceptions for expected failures.

This package is largely a port of `AStar.Dev.Functional.Extensions`.

## Installation

```bash
dotnet add package AStarDev.FunctionalParadigm
```

All types live in the `AStarDev.FunctionalParadigm` namespace:

```csharp
using AStarDev.FunctionalParadigm;
```

## Types

| Type | Purpose |
| ---- | ------- |
| `Result<TResult, TError>` | An operation that either succeeded (`Ok`) or failed (`Fail`) with a typed error. |
| `Option<T>` | A value that is either present (`Some`) or absent (`None`). Use instead of returning `null`. |
| `Validation<T>` | Either `Valid` or `Invalid` with a list of `ValidationError`s, so every problem is reported at once. |
| `Exceptional<T>` | Wraps code that may throw: `Success` or `Failure` carrying the `Exception`. |
| `Unit` | A "no value" type for use where a generic type argument is required. |

## Result

```csharp
Result<int, string> parsed = int.TryParse(input, out var number)
    ? Result.Success<int, string>(number)
    : Result.Failure<int, string>("Not a number");

string message = parsed
    .Map(value => value * 2)
    .Bind(value => value > 100
        ? Result.Failure<int, string>("Too large")
        : Result.Success<int, string>(value))
    .Match(
        onSuccess: value => $"Result: {value}",
        onFailure: error => $"Error: {error}");
```

Async chains work directly on `Task<Result<,>>` and `ValueTask<Result<,>>`, so there is no need to await into a variable first:

```csharp
string message = await service.GetAsync(cancellationToken)
    .BindAsync(item => service.EnrichAsync(item))
    .MatchAsync(
        onSuccess: item => Task.FromResult($"Loaded {item}"),
        onFailure: error => Task.FromResult($"Failed: {error}"));
```

Other helpers include `MapAsync`, `Tap`/`TapAsync`, `TapError`, `OrElseAsync` and `Ensure`/`EnsureAsync`. `RetryExtensions.RetryOnceAsync` retries a failed `Result` operation once.

## Option

```csharp
Option<User> user = Option.Some(currentUser);
Option<User> nobody = Option.None<User>();

string name = user
    .Filter(candidate => candidate.IsActive)
    .Map(candidate => candidate.Name)
    .MapOrDefault(value => value, "Unknown");

Result<User, string> asResult = nobody.ToResult(() => "User not found");
```

`ToOption()` converts nullable and reference values, and `Values()` / `Choose(...)` filter collections of options.

## Validation

```csharp
Validation<string> name = string.IsNullOrWhiteSpace(input)
    ? Validation.Invalid<string>(new ValidationError("Name", "Name is required"))
    : Validation.Valid(input);

Result<string, string> asResult = name.ToResult(errors => string.Join("; ", errors.Select(error => error.Message)));
```

Use `Apply` and `Combine` to gather the errors from several validations into one result.

## Exceptional

```csharp
Exceptional<string> content = Exceptional.Run(() => File.ReadAllText(path));
Exceptional<string> asyncContent = await Exceptional.RunAsync(() => File.ReadAllTextAsync(path));
```

## Pipe and compose

```csharp
int length = "hello".Pipe(text => text.ToUpperInvariant()).Pipe(text => text.Length);
```

`Pipe`, `PipeAsync`, `Tap` and `Compose` support small, readable pipelines.

## Notes

- Async extension methods use `ConfigureAwait(false)`, so they are safe to call from contexts with a synchronization context.
- XML documentation ships with the package for IntelliSense.

## License

MIT. Source and issues: <https://github.com/astar-development/astar-dev-mono/tree/main/packages/core>
