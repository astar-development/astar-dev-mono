# Constants

**Namespace:** `AStar.Dev.Utilities`
**File:** `Constants.cs`

Provides shared, pre-configured `JsonSerializerOptions` instances so callers don't have to construct them repeatedly.

---

## Members

### `WebDeserialisationSettings`

```csharp
public static JsonSerializerOptions WebDeserialisationSettings { get; }
```

Returns a `JsonSerializerOptions` instance configured with `JsonSerializerDefaults.Web`:

- Property names are matched case-insensitively.
- Numbers may be read from JSON strings.
- Property naming policy is camelCase.

---

## Example

```csharp
using AStar.Dev.Utilities;
using System.Text.Json;

var json = """{ "firstName": "Ada", "age": 36 }""";
var person = JsonSerializer.Deserialize<Person>(json, Constants.WebDeserialisationSettings);
```
