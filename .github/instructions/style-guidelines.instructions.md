---
applyTo: "**/*.cs"
---

Coding standards and style guidelines / preferences for C# files in this repository that AI must follow.

## Naming Conventions

- **Public members**: PascalCase (e.g., `MyClass`, `MyMethod`, `MyProperty`)
- **Private members**: camelCase (e.g., `myVariable`, `myMethod`)
- **Private fields**: \_camelCase with underscore prefix (e.g., `_fieldName`)
- **Constants**: CONSTANT_CASE or PascalCase
- Use meaningful names that clearly convey purpose; avoid abbreviations unless widely understood
- Use `nameof()` for parameter names in exceptions and logging

## Namespaces

- Use file-scoped namespaces.
- Namespace names should follow the pattern: Company.Project.Module (e.g., Contoso.Sales.Reporting).
- Avoid unnecessary nested namespaces; keep the structure flat when possible.

## Classes and Methods

- Define one class per file, and name the file after the class.
- Follow SOLID principles for class and method design.
- Keep classes focused on a single responsibility.
- Ensure good cohesion within classes and methods (related functionality grouped together).
- Ensure low coupling between classes and methods.
- Ensure methods do one thing and do it well.
- Keep methods short; ideally under 20 lines.
- Use meaningful names for classes and methods that clearly convey their purpose.
- Put all method / constructor overloads together in the same order as their parameters.
- Put all method / constructor parameters on one line when possible; otherwise, wrap parameters to multiple lines but avoid 1 parameter per line.
- Use expression-bodied members for simple methods and properties.
- Keep method and constructor parameters to a minimum (ideally <5 parameters); prefer using parameter objects when multiple parameters are needed.
- Avoid long parameter lists; consider using the Builder pattern for complex object construction.
- Use dependency injection for managing dependencies (no newing up services inside classes).
- Prefer composition over inheritance.
- Avoid deep nesting; use early returns and guard clauses.
- Do not use regions or #pragma to hide code; refactor instead.
- Never comment within methods or private members; if a comment is needed, it likely indicates the method is doing too much and should be refactored into smaller, more focused methods. Instead of comments, strive for self-explanatory code through clear naming and small method sizes.

## Immutability

- Prefer immutable data structures and objects where possible.
- Prefer `record` types for immutable data models and DTOs.
- Use `class` for entities with behavior or mutable state.
- Use `init` properties when immutability is desired.
- Use immutable collections: `IReadOnlyList<T>`, `IReadOnlyCollection<T>`, `IReadOnlyDictionary<TKey, TValue>`.
- Use `with` expressions to create modified copies of immutable objects.

## Record Design

- Define record properties on the same line with the record declaration when possible.
- Accompany each record `<name>` with a corresponding `<name>Factory` static factory class.
- Place the factory class in the same file as the record it creates.
- Expose static `Create` methods on the factory class for constructing instances of the record.
- Place argument validation logic within the factory methods.
- Never use the public constructor of a record directly; always use the factory methods.
- Use immutable collections (e.g., `IReadOnlyList<T>`, `IReadOnlyDictionary<TKey, TValue>`) for record properties that hold multiple values.
- Avoid methods on records; use extension methods instead for any behavior related to the record.

## Discriminated Unions

- Use records with inheritance to model discriminated unions.
- Define an abstract base record for the union type and derive specific case records from it.
- Place all case records in the same file as the base record.
- Define one static factory class per discriminated union type.
- Expose static `Create` methods on the factory class for constructing instances of each case record.

## Variables and Constants

- Use `var` when the type is obvious from the right-hand side; otherwise use explicit types for clarity.
- Use `const` for compile-time constants.
- Use `static readonly` for runtime constants.
- Avoid magic strings and numbers; use constants or enums instead.

## Collections and Data Structures

- Use `IEnumerable<T>` for collections that do not require indexing.
- Use `IReadOnlyList<T>` or `IReadOnlyCollection<T>` when immutability is desired.
- Use `StringBuilder` for concatenating multiple strings in loops or when performance is critical; otherwise use string interpolation for readability.
- Use Collection Initializers and Object Initializers for cleaner code when creating collections and objects.

## Control Flow and Logic

- Use pattern matching and switch expressions for clearer and more concise code when dealing with multiple conditions.
- Null checking with null-coalescing operators (`??`, `??=`, `?.`).
- Use `ArgumentNullException` (or `ArgumentNullException.ThrowIfNull`) in public constructors and methods.

## Test Classes

- Name test classes with the suffix `Should` (e.g., `OrderServiceShould`).
- Organize test methods using the `<Action><Result>[<Exception>]` naming convention (e.g., `ReturnCorrectSumWhenMultipleItemsExist`). This pattern helps clarify the action being tested, the expected result, and any exceptions that may be thrown.
- Use the Arrange-Act-Assert (AAA) pattern within test methods to structure the code clearly. Divide the method into three distinct sections: setup (Arrange), execution (Act), and verification (Assert). Do not comment these sections; the structure should be clear from the code itself. Separate these sections with a single blank line for readability.
- Use test data builders to create complex test objects, enhancing readability and maintainability of tests.
- Avoid logic in test methods; keep tests simple and focused on behavior verification.
- Use Shouldly for more readable and expressive assertions in tests.
- Use NSubstitute for mocking dependencies in tests. Avoid mocking when possible; prefer using real instances or test doubles.
- Never add XML documentation or comments to test classes or test methods.
