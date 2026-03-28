---
title: 'Clean Architecture in Practice: Lessons From Real .NET Projects'
date: 2026-02-18
summary: 'Clean Architecture looks elegant on diagrams. Here is what it actually looks like after a year on a production .NET system — the wins, the friction, and what I would do differently.'
tags: ['Architecture', '.NET', 'Clean Architecture', 'Design Patterns']
draft: false
---

Clean Architecture diagrams are compelling. Concentric circles, arrows pointing inward, dependencies flowing toward the domain. On a whiteboard it makes obvious sense. Then you start the project, and the questions begin.

Where does validation live? Is a `Result<T>` type infrastructure or domain? Who owns the mapping between the persistence model and the domain entity? After several years of shipping .NET systems structured this way, I have some answers — and some honest admissions about where the model breaks down.

## What Actually Works

**The dependency rule is worth the discipline.** Forcing all dependencies to point inward — infrastructure depends on application, application depends on domain, domain depends on nothing — produces codebases that are genuinely easier to test. When I need to swap a SQL Server repository for an in-memory one in a test, it is a one-line constructor change, not a refactor.

**Use cases as the unit of work.** Structuring application logic as explicit use case classes (`ProcessOrderUseCase`, `GenerateInvoiceUseCase`) rather than bloated service classes keeps things comprehensible. Each class has one public method, one reason to exist. Onboarding a new developer to a feature means showing them one file.

```csharp
public sealed class ProcessOrderUseCase
{
    private readonly IOrderRepository _orders;
    private readonly IPaymentGateway _payments;
    private readonly IEventBus _events;

    public ProcessOrderUseCase(
        IOrderRepository orders,
        IPaymentGateway payments,
        IEventBus events)
    {
        _orders = orders;
        _payments = payments;
        _events = events;
    }

    public async Task<Result<OrderConfirmation>> ExecuteAsync(ProcessOrderCommand command, CancellationToken ct = default)
    {
        var order = await _orders.GetByIdAsync(command.OrderId, ct);
        if (order is null) return Result.Failure<OrderConfirmation>("Order not found.");

        var paymentResult = await _payments.ChargeAsync(order.Total, command.PaymentToken, ct);
        if (!paymentResult.IsSuccess) return Result.Failure<OrderConfirmation>(paymentResult.Error);

        order.MarkAsPaid();
        await _orders.SaveAsync(order, ct);
        await _events.PublishAsync(new OrderPaidEvent(order.Id), ct);

        return Result.Success(new OrderConfirmation(order.Id, order.Total));
    }
}
```

**The domain model as the source of truth.** When invariants live in the domain — inside the `Order` class, enforced in the constructor or in methods — bugs stay caught rather than scattered across service layers. An `Order` that cannot be constructed in an invalid state is worth more than ten validators.

## What Creates Friction

**Mapping overhead.** You will write mappings. Between the persistence model and the domain entity. Between the domain entity and the DTO. Between the DTO and the API response model. This is the price of clean separation, and it is real. Tools like Mapperly reduce the boilerplate, but the boundary crossings still exist.

**Over-engineering small features.** A simple lookup endpoint — "give me all active products" — does not need a use case class, a query handler, a repository abstraction, and a domain entity. Sometimes a thin query directly from the controller to a read model is correct. Clean Architecture does not prohibit pragmatism; it just does not advertise it.

**Where does this go?** Validation, mapping, cross-cutting concerns, domain events, notification handlers — the architecture describes layers, not every class. Expect debates. Have them early, document the decision, and move on. An ADR (Architecture Decision Record) for "where do we put validators?" sounds over the top until the third time you answer the same question in a PR review.

## What I Would Do Differently

**Start simpler, add structure as complexity warrants.** On a project with three aggregates and two developers, the full Clean Architecture ceremony is overhead. Start with clear naming conventions and the dependency rule. Add explicit use case classes when services start accumulating too many methods.

**Invest in a shared `Result<T>` type early.** The tension between exceptions and return values runs through every layer. Picking a convention at the start — I use a simple `Result<T>` with `IsSuccess`, `Value`, and `Error` — saves arguments and inconsistencies across the life of the project.

**Write integration tests at the use case boundary.** Unit tests on domain entities are valuable. But the highest-value tests hit the use case with a real (in-memory or containerised) database and assert on the observable outcome. That is the contract that matters in production.

Clean Architecture is not a silver bullet. It is a set of constraints that tend to produce good outcomes when applied with judgement. The diagram is a starting point, not a specification. The job is to understand why the rules exist, then decide when breaking them is the right call.
