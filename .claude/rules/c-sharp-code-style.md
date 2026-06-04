---
paths:
    - "**/*.cs"
---

## Naming

- Private fields: camelCase, no underscore prefix (`fieldName` not `_fieldName`)
- `nameof()` for parameter names in exceptions and logging
- No single-letter variables except loop indices. Name for meaning: `customerId` not `id`.

## Structure

- File-scoped namespaces. One type per file.
- Single-line method/constructor signatures. Split ONLY at >200 chars.
- Blank line before `return` after a code block. NOT after `if`/`else`.
- Expression-bodied members for simple methods/properties.
- No regions, no `#pragma` to hide code.
- Early returns / guard clauses over deep nesting.
- No newing up services; use DI.
- Explicit types when non-obvious; `var` only when the type is clear from the right-hand side.

## Primitive Obsession

- IDs must be strongly-typed — use `AStar.Dev.Source.Generators.Attributes`.
- File/directory info must NOT be a string — use Testably abstraction or dedicated type.

## Records

- Accompany every `record Foo` with `FooFactory` static class exposing `Create` methods.
- Validation in factory, never in constructor. Never use the public constructor directly.
- No methods on records — use extension methods.
- Immutable collection properties: `IReadOnlyList<T>`, `IReadOnlyDictionary<K,V>`.

## Discriminated Unions

- Abstract base record + derived case records, all in one file.
- One `<Name>Factory` class per union, `Create` methods per case.

## Immutability

- `record` for immutable models/DTOs; `class` for entities with mutable state.
- Immutable collections for all multi-value properties and return types.

## Tests

- Class naming: `Given<Context>` — e.g. `GivenAnAccount`, `GivenADatabaseReadyForSync`
- Method naming: `when_[action]_then_[outcome]` snake_case
- AAA pattern, blank lines between sections, no section comments.
- Shouldly assertions. NSubstitute mocks (prefer real instances).
- No comments anywhere in test files — no XML docs, no inline comments, no block comments. Name the test and the variables clearly enough that no comment is needed.

## Error Handling

- **Public APIs and factories**: Never throw exceptions for invalid input. Use `Result<T>` from `AStar.Dev.Functional.Extensions` to represent success/failure.
- **Internal operations**: Exceptions are OK for truly exceptional conditions (e.g., null reference after null-check failed = bug in code).
- **Factory methods**: Normalize invalid input gracefully instead of throwing. Example: `FileClassificationFactory.Create("")` normalizes empty string to a sensible default rather than throwing.
- **Return types**: `Result<TSuccess, TError>` or `Option<T>` communicate intent; callers can't accidentally ignore errors.

## Functional Extensions

Never await `Task<Result<T,E>>` into a variable then call `.Match()` — chain `.MatchAsync()` directly:
```csharp
// ❌ var result = await service.GetAsync(ct); result.Match(...)
// ✅ await service.GetAsync(ct).MatchAsync(ok => ..., err => ...)
```
Error-branch logic belongs INSIDE the error lambda. Never use `is Result<T,E>.Ok` pattern matching.

## Utilities

Use `AStar.Dev.Utilities` helpers. e.g. `"dir1".CombinePaths("dir2")` not `Path.Combine(...)` — `Path.Combine` silently drops parameters.
