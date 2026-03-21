# Linq Extensions

**Namespace:** `AStar.Dev.Utilities`
**File:** `LinqExtensions.cs`

Additional LINQ-style extension methods for `IEnumerable<T>`.

---

## Methods

### `ForEach<T>`

```csharp
public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
```

Executes `action` for each element in `enumerable`. Both the sequence and the action are null-guarded — if either is `null` the call is a no-op.

| Parameter | Type | Description |
|-----------|------|-------------|
| `enumerable` | `IEnumerable<T>` | The sequence to iterate. |
| `action` | `Action<T>` | The delegate to invoke for each element. |

---

## Examples

### Basic iteration

```csharp
using AStar.Dev.Utilities;

var names = new[] { "Alice", "Bob", "Carol" };
names.ForEach(n => Console.WriteLine(n));
// Alice
// Bob
// Carol
```

### Collecting side-effects

```csharp
using AStar.Dev.Utilities;

var results = new List<string>();
new[] { "x", "y", "z" }.ForEach(s => results.Add(s.ToUpper()));
// results: ["X", "Y", "Z"]
```

### Null safety

```csharp
using AStar.Dev.Utilities;

IEnumerable<int>? nullSequence = null;
nullSequence.ForEach(i => Console.WriteLine(i)); // no-op, no NullReferenceException
```
