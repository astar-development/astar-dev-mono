# Encryption Extensions

**Namespace:** `AStar.Dev.Utilities`
**File:** `EncryptionExtensions.cs`

AES-based encryption and decryption extension methods for `string`.

Both methods work symmetrically: a string encrypted with a given key/IV pair can only be decrypted with the same pair. The encrypted value is returned as a Base64 string, making it safe to store or transmit in text contexts.

> **Note:** If you omit the `key` and `iv` parameters, a built-in default pair is used. For anything security-sensitive, supply your own 32-byte key and 16-byte IV.

---

## Methods

### `Encrypt`

```csharp
public static string Encrypt(this string plainText, string? key = null, string? iv = null)
```

Encrypts `plainText` with AES and returns the result as a Base64 string.

| Parameter | Type | Description |
|-----------|------|-------------|
| `plainText` | `string` | The text to encrypt. |
| `key` | `string?` | Optional. Must be 16, 24, or 32 bytes (UTF-8). Falls back to an internal default (32 bytes) if omitted. |
| `iv` | `string?` | Optional. Must be exactly 16 bytes (UTF-8). Falls back to an internal default if omitted. |

**Returns:** Base64-encoded cipher text.

---

### `Decrypt`

```csharp
public static string Decrypt(this string encryptedText, string? key = null, string? iv = null)
```

Decrypts a Base64-encoded AES cipher text back to the original plain text.

| Parameter | Type | Description |
|-----------|------|-------------|
| `encryptedText` | `string` | The Base64-encoded cipher text produced by `Encrypt`. |
| `key` | `string?` | Optional. Must match the key used during encryption. |
| `iv` | `string?` | Optional. Must match the IV used during encryption. |

**Returns:** The original plain-text string.

---

## Examples

### Using the built-in defaults

```csharp
using AStar.Dev.Utilities;

string original  = "Hello, World!";
string encrypted = original.Encrypt();
string decrypted = encrypted.Decrypt();

Console.WriteLine(decrypted); // Hello, World!
```

### Using a custom key and IV

```csharp
using AStar.Dev.Utilities;

string key = "MyCustomKey12345MyCustomKey12345"; // 32 bytes
string iv  = "MyCustomIV123456";                 // 16 bytes

string encrypted = "Hello, World!".Encrypt(key, iv);
string decrypted = encrypted.Decrypt(key, iv);

Console.WriteLine(decrypted); // Hello, World!
```
