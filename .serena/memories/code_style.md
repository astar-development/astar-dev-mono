## C# Code Style & Conventions

**Naming:**
- Public: PascalCase
- Private: camelCase (no underscore prefix)
- Constants: PascalCase
- No single-letter vars except loop indices (i, j, k)
- Use `nameof()` in exceptions/logging

**Methods & Classes:**
- Single-line signatures always (no param splitting regardless of length)
- One class per file, named after class
- Max method: ~20 lines
- Max class: ~300 lines
- Extract methods instead of inline comments
- Blank line before every `return` (except directly after if/else)

**Immutability:**
- Prefer `record` for data models/DTOs
- Use `class` for entities with behavior
- Use `init` properties
- Immutable collections: IReadOnlyList<T>, IReadOnlyDictionary<K,V>
- Factory classes: `<Name>Factory` with static `Create()` methods

**Records:**
- Properties on same line as declaration
- Never call public constructor—use factory methods
- Discriminated unions via record inheritance (abstract base + case records in same file)

**Collections:**
- IEnumerable<T>: no indexing needed
- IReadOnlyList<T> / IReadOnlyCollection<T>: immutability desired
- StringBuilder: only for loop concat or perf-critical
- Collection initializers for clarity

**Testing (Given prefix, when_then snake_case):**
- Class: `GivenAnAccount`, `GivenADatabaseReadyForSync`
- Method: `when_deleted_then_all_linked_rows_are_removed`
- Pattern: Arrange-Act-Assert (no comments for sections)
- Use Shouldly assertions + NSubstitute mocks

**Comments:**
- NEVER restate what code says
- Only comment WHY if non-obvious
- No comments in test classes
- No comments in private methods
