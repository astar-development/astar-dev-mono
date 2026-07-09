---
description: "Use when writing or refactoring C# in this repo. Enforces modern C# syntax (latest supported language features) and functional patterns with Result/Option workflows."
name: "C# Modern Functional Conventions"
applyTo: "**/*.cs"
---

# C# Modern Functional Conventions

Use these rules in addition to existing repo C# style guidance.
These are hard rules and apply to production and test C# files.

## Modern C# Syntax

- MUST use the latest C# features supported by the solution when they improve readability.
- MUST use collection expressions (`[]`) where equivalent intent is clear.
- MUST use target-typed `new()` where the type is obvious from context.
- MUST prefer switch expressions and property/list patterns over long `if` or `switch` statement chains.
- MUST use `required` members and `init` setters for immutable construction flows.
- MUST use primary constructors when constructor parameters only initialize dependencies or state and no extra construction logic is required.
- MUST use raw string literals for multi-line content to avoid noisy escaping.
- NEVER choose a newer syntax form when it reduces clarity.

## Functional-First Error Handling

- For invalid input in public APIs and factories, NEVER throw. Return `Result<T>` or `Result<TSuccess, TError>`.
- MUST use `Option<T>` for optional values instead of nullable sentinel logic when domain intent is optionality.
- MUST model flows with `Map`, `Bind`, and `Match`/`MatchAsync` rather than nested conditionals.
- MUST chain async result handling directly: `await operation.MatchAsync(...)`.
- MUST NOT await into a temporary variable just to call `Match()` afterward.
- MUST keep success and error behavior inside the corresponding `Match`/`MatchAsync` lambdas.

## Functional Composition

- MUST keep methods small and composable: each method should transform input to output with minimal side effects.
- MUST prefer pure helper methods for domain transformations.
- MUST push I/O to boundaries (adapters/services) and keep domain logic deterministic.
- MUST favor immutable records and non-mutating transformations (`with` expressions) when shaping domain data.

## Minimal Example

```csharp
public async Task<Result<FileClassification>> ClassifyAsync(string? rawName, CancellationToken cancellationToken)
{
    return await FileClassificationFactory
        .Create(rawName)
        .Bind(name => _classifier.GetClassificationAsync(name, cancellationToken))
        .MatchAsync(
            success => Result.Success(success),
            error => Result.Failure<FileClassification>(error));
}
```
