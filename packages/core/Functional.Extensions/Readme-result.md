# 📦 Result<TSuccess, TError> - Functional Error Handling for C#

A powerful, functional approach to error handling in C# that avoids exceptions and promotes predictable, composable code.

## 🧭 Overview

`Result<TSuccess, TError>` is a discriminated union type that represents either successful completion with a value or failure with an error. This approach to error handling:

- ✅ Makes error handling explicit in your function signatures
- ✅ Encourages composition of operations that might fail
- ✅ Eliminates the need for try/catch blocks across your codebase
- ✅ Provides comprehensive async support
- ✅ Allows for functional programming patterns in C#

## 📚 Documentation

- [Core Concepts](docs/core-concepts.md)
- [Basic Usage Guide](docs/basic-usage.md)
- [Advanced Usage](docs/advanced-usage.md)
- [Method Reference](docs/method-reference.md)
- [Error Handling Patterns](docs/error-handling-patterns.md)
- [Testing with Results](docs/testing.md)

## 🚀 Quick Start

Install the package from NuGet:

```bash
dotnet add package AStar.Dev.Functional.Extensions
```

Basic usage:

``` csharp
using AStar.Dev.Functional.Extensions;

// Create a success result
Result<string, string> successResult = new Result<string, string>.Ok("Harry Potter");

// Create an error result
Result<decimal, string> errorResult = new Result<decimal, string>.Error("Book out of stock");

// Match on result to handle both success and error cases
string message = orderResult.Match(
    onSuccess: order => $"Order #{order.Id} confirmed: {order.Total:C}",
    onFailure: error => $"Order failed: {error}"
);
```

## 📋 Features

- Discriminated union representing success or failure
- Comprehensive set of transformation methods (Map, Bind, etc.)
- Full async support with all combination of sync/async operations
- Side-effect methods for logging and monitoring (Tap, TapError)
- Clear, functional approach to error handling
- Zero dependencies

## 📄 License

MIT
