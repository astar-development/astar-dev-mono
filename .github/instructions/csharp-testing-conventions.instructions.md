---
description: "Use when writing or refactoring C# tests. Enforces Given/when_then naming, strict AAA layout, and Shouldly plus NSubstitute test patterns."
name: "C# Testing Conventions"
applyTo: "apps/**/*Test*.cs, packages/**/*Test*.cs, tests/**/*Test*.cs"
---

# C# Testing Conventions

Use these rules for all C# test files.

## Naming

- Test classes MUST use Given<Context> naming.
- Test methods MUST use when*[action]\_then*[outcome] snake_case naming.
- Test names MUST describe behavior, not implementation details.

## Test Structure

- Tests MUST follow Arrange Act Assert structure.
- Arrange, Act, and Assert sections MUST be separated by a single blank line.
- Tests MUST NOT include comments, including AAA section comments.
- Each test MUST verify one behavior.

## Assertions and Doubles

- Assertions MUST use Shouldly.
- Mocks and substitutes MUST use NSubstitute.
- Prefer real objects over mocking where practical.

## Quality Rules

- Tests MUST be deterministic and independent.
- Tests MUST NOT rely on execution order.
- Time, file system, and external dependencies MUST be abstracted or controlled.
