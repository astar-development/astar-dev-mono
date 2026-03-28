---
title: 'TDD in .NET: Why It Still Matters'
date: 2026-03-10
summary: 'Red-green-refactor is not a ceremony. Here is why disciplined TDD produces better .NET systems and how to make it stick on real teams.'
tags: ['TDD', '.NET', 'Testing', 'Clean Code']
draft: false
---

Test-driven development has been around long enough that it should not need defending. Yet on almost every engagement I join, the tests are an afterthought — written after the code, shaped around the implementation, and broken on the first refactor. That is not TDD. That is test-after development with extra steps.

## The Core Misunderstanding

TDD is not about having tests. It is about using tests to **drive design**. When you write a failing test first, you are forced to think about the interface before the implementation. You cannot write a test for a class that is impossible to construct in isolation. The discipline reveals coupling before it calculates.

```csharp
// Bad: implementation-first, test bolted on later
public class OrderService
{
    private readonly SqlOrderRepository _repository;

    public OrderService()
    {
        _repository = new SqlOrderRepository(ConnectionString.Default);
    }
}

// Good: test-first forces an explicit dependency
public class OrderService
{
    private readonly IOrderRepository _repository;

    public OrderService(IOrderRepository repository)
    {
        _repository = repository;
    }
}
```

The second version did not emerge from a code review or an architecture discussion. It emerged because the test file could not construct the first version without spinning up a SQL Server.

## Why Teams Abandon It

TDD feels slow at the start of a feature. You write a test that does nothing, make it compile, watch it fail, write the minimum code to pass, then refactor. That loop takes longer than just writing the code.

The mistake is measuring the wrong thing. The cost is not in the writing — it is in the debugging, the production incidents, and the refactors that break unrelated tests three months later. On any project longer than a sprint, disciplined TDD pays back with interest.

The other reason teams abandon it: no one modelled it. TDD is a skill. If the most experienced engineer on the team writes code first and tests later, everyone does the same. The culture sets the floor.

## Making It Stick

Three things that actually work:

**1. Pair on the first few cycles.** Watching someone else do red-green-refactor — seeing how they handle the awkward test setup, how they name the test, how they decide when to refactor — is worth more than any blog post including this one.

**2. Make the feedback loop fast.** If running the test suite takes four minutes, no one will run it on every save. A domain project with no database dependencies should run in under two seconds. Invest in that.

**3. Track the red phase.** On pull request review, ask to see the failing test commit. If there is not one, the TDD was skipped. Not as a punishment — as a nudge. Once the team knows you will look for it, the habit forms.

## The .NET Tooling Is Good

xUnit, NUnit, and MSTest are all solid choices. The assertion libraries (FluentAssertions, Shouldly) reduce noise. Moq and NSubstitute handle mocking without ceremony. Testcontainers spins up real databases in Docker for integration tests. The infrastructure for TDD in .NET is mature and well-documented.

The only missing ingredient is the habit.

Start your next feature with a failing test. Name it so it reads like a requirement. Make it pass with the simplest code that could work. Then refactor. Repeat until done.

That is it. That is all TDD is.
