# Regex Extensions

**Namespace:** `AStar.Dev.Utilities`
**File:** `RegexExtensions.cs`

Character-class validation extension methods for `string`, backed by source-generated (`[GeneratedRegex]`) compiled regular expressions. All patterns use `RegexOptions.CultureInvariant` with a 1 000 ms timeout.

These are particularly useful for password-strength validation or input sanitisation.

---

## Methods

### `ContainsAtLeastOneLowercaseLetter`

```csharp
public static bool ContainsAtLeastOneLowercaseLetter(this string value)
```

Returns `true` if `value` contains one or more ASCII lowercase letters (`a–z`).

---

### `ContainsAtLeastOneUppercaseLetter`

```csharp
public static bool ContainsAtLeastOneUppercaseLetter(this string value)
```

Returns `true` if `value` contains one or more ASCII uppercase letters (`A–Z`).

---

### `ContainsAtLeastOneDigit`

```csharp
public static bool ContainsAtLeastOneDigit(this string value)
```

Returns `true` if `value` contains one or more decimal digits (`0–9`).

---

### `ContainsAtLeastOneSpecialCharacter`

```csharp
public static bool ContainsAtLeastOneSpecialCharacter(this string value)
```

Returns `true` if `value` contains one or more special characters. The matched set covers printable non-alphanumeric ASCII characters including `!`, `"`, `#`, `$`, `%`, `&`, `'`, `(`, `)`, `*`, `+`, `,`, `-`, `.`, `/`, `:`, `;`, `<`, `=`, `>`, `?`, `@`, `[`, `\`, `]`, `^`, `_`, `` ` ``, `{`, `|`, `}`, `~`, and `¬`.

---

## Examples

### Password strength check

```csharp
using AStar.Dev.Utilities;

string password = "P@ssw0rd";

bool hasLower   = password.ContainsAtLeastOneLowercaseLetter();  // true
bool hasUpper   = password.ContainsAtLeastOneUppercaseLetter();  // true
bool hasDigit   = password.ContainsAtLeastOneDigit();            // true
bool hasSpecial = password.ContainsAtLeastOneSpecialCharacter(); // true

bool isStrong = hasLower && hasUpper && hasDigit && hasSpecial;  // true
```

### Validation helper

```csharp
using AStar.Dev.Utilities;

public static IEnumerable<string> GetPasswordErrors(string password)
{
    if (!password.ContainsAtLeastOneLowercaseLetter())
        yield return "Must contain at least one lowercase letter.";

    if (!password.ContainsAtLeastOneUppercaseLetter())
        yield return "Must contain at least one uppercase letter.";

    if (!password.ContainsAtLeastOneDigit())
        yield return "Must contain at least one digit.";

    if (!password.ContainsAtLeastOneSpecialCharacter())
        yield return "Must contain at least one special character.";
}
```
