# Tester Reference (Supplementary)

This file provides optional depth for the Tester agent. The enforceable rules are in:

- `.github/agents/Tester.agent.md`
- `.claude/CLAUDE.md`

## Quick Test Templates

### Unit test template

```csharp
public sealed class ComponentShould
{
    [Fact]
    public void ReturnExpectedValueWhenInputIsValid()
    {
        var sut = new Component();

        var result = sut.DoWork("input");

        result.ShouldBe("expected");
    }
}
```

### Async test template

```csharp
[Fact]
public async Task ReturnFailureWhenCancelled()
{
    using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
    cts.Cancel();
    var sut = new Component();

    var result = await sut.RunAsync(cts.Token);

    result.IsFailure.ShouldBeTrue();
}
```

### NSubstitute interaction template

```csharp
[Fact]
public async Task CallRepositoryOnceWhenSaving()
{
    var repo = Substitute.For<IRepository>();
    var sut = new Service(repo);

    await sut.SaveAsync(TestContext.Current.CancellationToken);

    await repo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
}
```

## What Good Coverage Looks Like

- Happy path behaviour is asserted.
- Error/failure path behaviour is asserted.
- Boundary/edge inputs are asserted.
- Assertions verify outcomes, not internals.

## Test Review Checklist (Compact)

- Uses AAA structure.
- Names are behaviour-focused.
- Deterministic and isolated.
- No comments/TODOs in tests.
- Uses Shouldly and project conventions.
- Async tests use `TestContext.Current.CancellationToken`.

## When to Escalate to Integration/E2E

- Contract changes across module boundaries.
- Serialization/persistence/query behaviour.
- User-visible workflow spans multiple components.

## Functional Patterns (Astar)

- Prefer verifying `Result` success/failure states explicitly.
- Prefer verifying `Option` Some/None behaviour explicitly.
- Include branch assertions for mapped/bound outcomes when behaviour depends on composition.
