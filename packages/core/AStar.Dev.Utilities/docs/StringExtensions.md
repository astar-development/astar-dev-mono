# String Extensions

**Namespace:** `AStar.Dev.Utilities`
**File:** `StringExtensions.cs`

Fluent extension methods for `string` and `string?` covering null checks, JSON deserialization, file-type detection, and string manipulation.

---

## Methods

### Null / empty checks

#### `IsNull`

```csharp
public static bool IsNull(this string? value)
```

Returns `true` if `value` is `null`.

---

#### `IsNotNull`

```csharp
public static bool IsNotNull(this string? value)
```

Returns `true` if `value` is not `null`.

---

#### `IsNullOrWhiteSpace`

```csharp
public static bool IsNullOrWhiteSpace(this string? value)
```

Returns `true` if `value` is `null`, empty, or consists only of whitespace. Equivalent to `string.IsNullOrWhiteSpace(value)`.

---

#### `IsNotNullOrWhiteSpace`

```csharp
public static bool IsNotNullOrWhiteSpace(this string? value)
```

Returns `true` if `value` is not `null`, not empty, and not whitespace only.

---

### JSON deserialization

#### `FromJson<T>` (default options)

```csharp
public static T FromJson<T>(this string json)
```

Deserializes `json` to type `T` using `JsonSerializer.Deserialize<T>` with the default options.

| Parameter | Type | Description |
|-----------|------|-------------|
| `json` | `string` | A valid JSON string. |

**Returns:** A deserialized instance of `T`.

---

#### `FromJson<T>` (custom options)

```csharp
public static T FromJson<T>(this string json, JsonSerializerOptions options)
```

Deserializes `json` to type `T` using the supplied `options`.

| Parameter | Type | Description |
|-----------|------|-------------|
| `json` | `string` | A valid JSON string. |
| `options` | `JsonSerializerOptions` | Options to control deserialization behavior. |

**Returns:** A deserialized instance of `T`.

---

### File-type detection

#### `IsImage`

```csharp
public static bool IsImage(this string fileName)
```

Returns `true` if `fileName` ends with `.jpg`, `.jpeg`, `.png`, `.bmp`, or `.gif` (case-insensitive). Returns `false` for null or empty strings.

---

#### `IsNumberOnly`

```csharp
public static bool IsNumberOnly(this string fileName)
```

Returns `true` if every character in `fileName` is a digit (`0–9`), an underscore (`_`), or a period (`.`).

---

### String manipulation

#### `TruncateIfRequired`

```csharp
public static string TruncateIfRequired(this string value, int truncateLength)
```

Returns `value` truncated to `truncateLength` characters if it exceeds that length; otherwise returns `value` unchanged. Returns `value` unchanged if `value` is null/empty or `truncateLength` is zero or negative.

| Parameter | Type | Description |
|-----------|------|-------------|
| `value` | `string` | The string to potentially truncate. |
| `truncateLength` | `int` | The maximum allowed length. |

---

#### `RemoveTrailing`

```csharp
public static string RemoveTrailing(this string value, string removeTrailing)
```

Removes `removeTrailing` from the end of `value` if it is present (case-insensitive). Returns `value` unchanged if either argument is null/empty.

| Parameter | Type | Description |
|-----------|------|-------------|
| `value` | `string` | The string to process. |
| `removeTrailing` | `string` | The suffix to remove. |

---

## Examples

### Null / empty guards

```csharp
using AStar.Dev.Utilities;

string? name = GetNameFromSomewhere();

if (name.IsNullOrWhiteSpace())
    throw new InvalidOperationException("Name is required.");

// fluent guard in a condition
bool valid = name.IsNotNullOrWhiteSpace();
```

### JSON round-trip

```csharp
using AStar.Dev.Utilities;

record Person(string FirstName, int Age);

var person  = new Person("Ada", 36);
string json = person.ToJson();                    // from ObjectExtensions
var copy    = json.FromJson<Person>();            // default options

// or with custom options
var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var copy2 = json.FromJson<Person>(opts);
```

### File-type detection

```csharp
using AStar.Dev.Utilities;

"photo.jpg".IsImage();   // true
"photo.PNG".IsImage();   // true
"report.pdf".IsImage();  // false
"".IsImage();            // false

"12_34.56".IsNumberOnly();  // true
"abc".IsNumberOnly();       // false
```

### String manipulation

```csharp
using AStar.Dev.Utilities;

// TruncateIfRequired
"Hello, World!".TruncateIfRequired(5);  // "Hello"
"Hi".TruncateIfRequired(10);            // "Hi"

// RemoveTrailing
"https://example.com/".RemoveTrailing("/");   // "https://example.com"
"MyFile.cs".RemoveTrailing(".cs");            // "MyFile"
"MyFile.vb".RemoveTrailing(".cs");            // "MyFile.vb" (no match)
```
