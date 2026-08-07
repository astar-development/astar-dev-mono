# ImmutableDomain.EntityFrameworkCore

`ImmutableDomain.EntityFrameworkCore` is a companion library for Entity Framework Core that lets you persist aggregates built from immutable objects. Keep your domain expressive, with constructors, value objects, and `with`-style copy helpers, while EF Core handles persistence and change tracking under the hood.

## Highlights
- Model aggregates as immutable types without leaking setters for EF Core.
- Centralize eager-loading rules so every query hydrates a complete aggregate.
- Update graphs by value; the library diff-checks navigation collections and references, applying the right EF Core states automatically.
- Support primary and alternate keys (for natural identifiers) during lookups.

## Building Blocks
- `IImmutableEntityRepository<TEntity>` exposes `AddImmutableAsync`, `FindImmutableAsync`, `UpdateImmutable`, and `RemoveImmutable`.
- `DbContextExtensions.ToImmutableEntityRepository(...)` wraps a `DbSet<T>` plus include paths so your `DbContext` can return ready-to-use repositories.
- `ImmutableUpdateExtensions.UpdateImmutable(...)` reconciles tracked entities with immutable copies, aligning collections, references, and entity states.
- `KeyExpression<TEntity>` evaluates equality against primary or alternate keys, enabling `FindImmutableAsync` calls that use natural IDs (like public identifiers) as stable keys.

## Demo flow
See [Demo/Program.cs](Demo/Program.cs) for the comprehensive demonstration:
1. `EnsureDeletedAsync` + `MigrateAsync` reset the SQL Server schema.
2. An immutable `Invoice` aggregate (with `InvoiceNumber`, `Currency`, and `InvoiceLine` value objects) is persisted via `AddImmutableAsync`.
3. The aggregate is reloaded via `FindImmutableAsync`, cloned using domain `With*` helpers, and saved through `UpdateImmutable`.
4. Console output compares the original tracked instance with the reloaded, updated one.

## Getting started
1. **Reference the project** – add a project reference or NuGet package (when published) for `ImmutableDomain.EntityFrameworkCore`.
2. **Expose repositories from your `DbContext`:**

```csharp
public class DatabaseDbContext : DbContext
{
    public IImmutableEntityRepository<Invoice> Invoices =>
        Set<Invoice>().ToImmutableEntityRepository(this, "Lines");
}
```

3. **Model aggregates as immutable types** using constructors, value objects, and `With*` methods.
4. **Use the repository in your application layer:**

```csharp
var invoice = new Invoice(number, "Big Joe", issuedOn, InvoiceStatus.Issued, usd)
    .WithLines([
        new InvoiceLine("Thinking Hat", 2, new Money(19.99m, usd)),
        new InvoiceLine("Sleeping Glasses", 1, new Money(29.99m, usd))
    ]);

await dbContext.Invoices.AddImmutableAsync(invoice);
await dbContext.SaveChangesAsync();

var current = await dbContext.Invoices.FindImmutableAsync(invoice.PublicId);
var updated = current!.WithCustomerName("Sleepy Sam")
    .WithLines(current.Lines.Add(new InvoiceLine("Invisibility Cloak", 1, new Money(99.99m, usd))));

dbContext.Invoices.UpdateImmutable(updated);
await dbContext.SaveChangesAsync();
```

### Repository guarantees
- Includes declared on `ToImmutableEntityRepository` always apply, ensuring fully populated aggregates when calling `FindImmutableAsync`.
- `UpdateImmutable` reattaches the immutable copy, aligns navigation graphs, and diff-checks child collections using stable keys (see [ImmutableDomain.EntityFrameworkCore/Implementation/ImmutableUpdateExtensions.cs](ImmutableDomain.EntityFrameworkCore/Implementation/ImmutableUpdateExtensions.cs)).
- Alternate keys (like `Invoice.PublicId`) work out of the box for natural lookups.

## Under the hood
- Stable composite keys are derived via reflection so entities can be matched without exposing mutable IDs.
- Collection updates respect cascade rules: removed children are deleted, unchanged ones stay `Unchanged`, and new ones become `Added`.
- You can define multiple repositories with different include graphs if you need variations (e.g., summaries vs detailed aggregates).

## Run the demo
```bash
dotnet run --project ./Demo
```

The console output shows immutable aggregates flowing through EF Core—from insert to update—without mutating the original instances.

## License
See [LICENSE](LICENSE).