# Object Extensions

**Namespace:** `AStar.Dev.Utilities`
**File:** `ObjectExtensions.cs`

Extension methods for serializing any object to JSON.

---

## Methods

### `ToJson<T>`

```csharp
public static string ToJson<T>(this T obj)
```

Serializes `obj` to a JSON string using `JsonSerializerDefaults.Web` with `WriteIndented = true`.

- Property names are written in camelCase.
- Output is human-readable (indented).

| Parameter | Type | Description |
|-----------|------|-------------|
| `obj` | `T` | The object to serialize. |

**Returns:** A pretty-printed JSON string.

---

## Examples

### Serialize a simple object

```csharp
using AStar.Dev.Utilities;

var person = new { FirstName = "Ada", Age = 36 };
Console.WriteLine(person.ToJson());
```

Output:
```json
{
  "firstName": "Ada",
  "age": 36
}
```

### Serialize a collection

```csharp
using AStar.Dev.Utilities;

var items = new[] { "alpha", "beta", "gamma" };
Console.WriteLine(items.ToJson());
```

Output:
```json
[
  "alpha",
  "beta",
  "gamma"
]
```

### Round-trip with `StringExtensions.FromJson`

```csharp
using AStar.Dev.Utilities;

var original = new MyRecord("Ada", 36);
string json  = original.ToJson();
var restored = json.FromJson<MyRecord>();
```
