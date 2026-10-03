# Exceptional&lt;T&gt; and the Try Helper

`Exceptional<T>` (in `AStarDev.FunctionalParadigm`) represents an operation that either succeeds with a `T` or fails with a captured `Exception`, without throwing. Never let exceptions escape a member whose return type is `Exceptional<T>` / `Task<Exceptional<T>>` — wrap the risky call with `Try.Run` or `Try.RunAsync` instead of hand-rolled `try/catch`.

```csharp
// sync
Exceptional<TAggregate> IRepository<TAggregate, TKey>.Add(TAggregate aggregate) =>
    Try.Run(() =>
    {
        Set<TAggregate>().Add(aggregate);
        return aggregate;
    });

// async
Task<Exceptional<Option<TAggregate>>> IRepository<TAggregate, TKey>.TryFindAsync(TKey key) =>
    Try.RunAsync(async () => (Option<TAggregate>)await Set<TAggregate>().FindAsync(key));
```

Key points:

- `Try.Run`/`Try.RunAsync` catch any thrown exception and lift it into a `Failure<T>`; a normal return value is lifted into `Success<T>`.
- `OperationCanceledException` (including `TaskCanceledException`) is never captured — it always rethrows, preserving cancellation semantics.
- If the delegate already returns `Exceptional<T>` (or the target type is itself `Exceptional<T>`), don't wrap the result again — `Try.RunAsync<T>` returns `Task<Exceptional<T>>` directly, so it can be returned as-is when `T` matches the interface's expected success type.
- Consumers should stay unaware of this - no per-call `try/catch` boilerplate is needed at call sites; `Exceptional<T>` is consumed via `.Match(onSuccess, onFailure)` or `.Tap(...)`.

See [packages/AStarDev.FunctionalParadigm/Try.cs](../packages/AStarDev.FunctionalParadigm/Try.cs) and its usage in [packages/AStarDev.ControlDb/ControlDbContext.cs](../packages/AStarDev.ControlDb/ControlDbContext.cs).
