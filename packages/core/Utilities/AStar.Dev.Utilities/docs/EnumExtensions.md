# Enum Extensions

**Namespace:** `AStar.Dev.Utilities`
**File:** `EnumExtensions.cs`

Extension methods for parsing strings into enum values.

---

## Methods

### `ParseEnum<T>`

```csharp
public static T ParseEnum<T>(this string value)
```

Parses `value` into the enum type `T`. The match is case-insensitive.

| Parameter | Type | Description |
|-----------|------|-------------|
| `value` | `string` | The string representation of the enum member to parse. |

**Returns:** The matching `T` enum value.

**Throws:** `ArgumentException` if `value` is not a valid member of `T`.

---

## Examples

```csharp
using AStar.Dev.Utilities;

public enum Colour { Red, Green, Blue }

Colour c1 = "green".ParseEnum<Colour>(); // Colour.Green
Colour c2 = "BLUE".ParseEnum<Colour>();  // Colour.Blue
Colour c3 = "Red".ParseEnum<Colour>();   // Colour.Red
```

### Handling unknown values

```csharp
using AStar.Dev.Utilities;

try
{
    Colour c = "yellow".ParseEnum<Colour>();
}
catch (ArgumentException ex)
{
    Console.WriteLine(ex.Message); // Requested value 'yellow' was not found.
}
```
