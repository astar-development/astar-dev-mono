---
description: "Use when creating or refactoring C# domain models. Enforces record factory patterns, discriminated unions, immutable modeling, and strong typed IDs."
name: "C# Domain Modeling Conventions"
applyTo: "**/*.cs"
---

# C# Domain Modeling Conventions

Use these rules when modeling domain concepts in C#.

## Strong Domain Types

- Primitive obsession MUST be avoided in domain boundaries.
- IDs MUST be strongly typed and MUST NOT be represented as raw string or Guid values in domain APIs.
- File and directory concepts MUST use dedicated types or approved abstractions, not plain strings.

## Records and Factories

- Immutable domain data SHOULD be modeled with records.
- Each record MUST have a matching <Name>Factory static class in the same file.
- Record construction MUST go through factory Create methods.
- Validation and normalization MUST be implemented in factory methods, not in constructors.

## Discriminated Unions

- Domain unions MUST use an abstract base record with derived case records.
- Union cases for one domain concept MUST be grouped in one file.
- Each union MUST have one <Name>Factory class with Create methods for each case.

## Immutability and Behavior

- Multi-value properties MUST use immutable collection interfaces.
- Domain transformations SHOULD prefer non-mutating updates and with expressions.
- Behavior attached to records SHOULD be implemented through extension methods when practical.

## Error and Optionality Modeling

- Invalid input in public domain factories MUST return Result failures, not exceptions.
- Absence of value MUST use Option where it reflects domain intent.
