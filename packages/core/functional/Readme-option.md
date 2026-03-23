# 📦 Option - Functional Optional Type for C#

A powerful, functional approach to handling optional values in C# that eliminates null reference exceptions and promotes predictable, composable code.

## 🧭 Overview

The Option<T> is a discriminated union type that represents either the presence of a value () or the absence of a value (). This approach to optional value handling: `Option<T>``Some``None`

- ✅ Makes the possibility of missing values explicit in your function signatures
- ✅ Encourages composition of operations that might not yield a value
- ✅ Eliminates null reference exceptions
- ✅ Provides comprehensive async support
- ✅ Allows for functional programming patterns in C#

## 📚 Core Concepts

### 🧱 What is Option?

The Option<T> is a discriminated union type that represents either the presence of a value or its absence. It is a powerful alternative to null-based programming and enables functional programming
patterns in C#.
`Option<T>`

### 🏗️ Structure

`Option<T>` encapsulates either:

- ✅ `Some(T)` — a present value
- ❌ `None` — the absence of a value

``` csharp
// Create a Some option
Option<string> someOption = new Option<string>.Some("Harry Potter");

// Create a None option
Option<decimal> noneOption = Option<decimal>.None.Instance;
```

### 🧰 Construction

| Expression                  | Description              |
|-----------------------------|--------------------------|
| `new Option<T>.Some(value)` | Constructs a Some option |
| `Option<T>.None.Instance`   | Gets the None instance   |
| `Option.Some(value)`        | Factory method for Some  |
| `Option.None<T>()`          | Factory method for None  |

### 🔍 Checking Option Type

You can use pattern matching to check the type of an option:

``` csharp
if (option is Option<Book>.Some some)
{
    // Handle Some case
    Book book = some.Value;
}
else if (option is Option<Book>.None)
{
    // Handle None case
}
```

### 🔄 Pattern Matching

The most common way to handle both Some and None cases is with the method: `Match`

``` csharp
string message = option.Match(
    onSome: book => $"Found: {book.Title} by {book.Author}",
    onNone: () => "No book found"
);
```

### 🌟 Benefits Over Nulls

1. **Explicit Optional Types**: The possibility of absence is part of the method signature
2. **No Null Reference Exceptions**: All possible cases are explicitly modeled
3. **Composition**: Options can be easily composed and chained
4. **Clarity**: Makes optional paths explicit and visible

## 📖 Basic Usage Guide

This guide demonstrates the fundamental patterns for using the type in a book store application context. `Option<T>`

### 📚 Finding a Book

Let's start with a simple function to find a book by ISBN:

``` csharp
public Option<Book> FindBook(string isbn)
{
    if (string.IsNullOrEmpty(isbn))
        return Option.None<Book>();
        
    Book book = _bookRepository.GetByIsbn(isbn);
    
    if (book == null)
        return Option.None<Book>();
        
    return Option.Some(book);
}
```

### 🔍 Using the Option

``` csharp
Option<Book> option = FindBook("978-0439708180");

// Match on option to handle both cases
string message = option.Match(
    book => $"Found: {book.Title} by {book.Author}",
    () => "Book not found"
);

// Display the message
Console.WriteLine(message);
```

### 🛒 Adding a Book to Cart

``` csharp
public Option<ShoppingCart> AddToCart(ShoppingCart cart, string isbn, int quantity)
{
    // Validate inputs
    if (cart == null)
        return Option.None<ShoppingCart>();
        
    if (quantity <= 0)
        return Option.None<ShoppingCart>();
    
    // Find the book
    Option<Book> bookOption = FindBook(isbn);
    
    // Match on the book option
    return bookOption.Match(
        book => 
        {
            // Add the book to cart
            cart.AddItem(new CartItem(book, quantity));
            return Option.Some(cart);
        },
        () => Option.None<ShoppingCart>()
    );
}
```

### 🔄 Using Map Instead of Match

We can simplify the above example using : `Map`

``` csharp
public Option<ShoppingCart> AddToCart(ShoppingCart cart, string isbn, int quantity)
{
    // Validate inputs
    if (cart == null)
        return Option.None<ShoppingCart>();
        
    if (quantity <= 0)
        return Option.None<ShoppingCart>();
    
    // Find the book and map to cart
    return FindBook(isbn).Map(book => 
    {
        cart.AddItem(new CartItem(book, quantity));
        return cart;
    });
}
```

### ⛓️ Chaining Operations with Bind

Let's implement a sequence of operations:

``` csharp
// Step 1: Find book by ISBN
public Option<Book> FindBook(string isbn) { /* ... */ }

// Step 2: Check availability
public Option<BookAvailability> CheckAvailability(Book book)
{
    var availability = _inventoryService.GetAvailability(book.Isbn);
    
    if (availability == null || availability.InStock == false)
        return Option.None<BookAvailability>();
        
    return Option.Some(availability);
}

// Step 3: Reserve book
public Option<BookReservation> ReserveBook(BookAvailability availability, int quantity)
{
    if (availability.Quantity < quantity)
        return Option.None<BookReservation>();
        
    var reservation = _reservationService.CreateReservation(
        availability.BookIsbn, quantity);
        
    return reservation != null 
        ? Option.Some(reservation) 
        : Option.None<BookReservation>();
}
```

Now we can chain these operations using : `Bind`

``` csharp
public Option<BookReservation> ReserveBookByIsbn(string isbn, int quantity)
{
    return FindBook(isbn)
        .Bind(book => CheckAvailability(book))
        .Bind(availability => ReserveBook(availability, quantity));
}
```

### 📝 Handling Side Effects with Tap

``` csharp
public Option<BookReservation> ReserveBookWithLogging(string isbn, int quantity)
{
    return ReserveBookByIsbn(isbn, quantity)
        .Tap(reservation => 
        {
            // Log successful reservation
            _logger.Info($"Book {isbn} reserved successfully: {reservation.ReservationId}");
        });
}
```

Want more? Some additional documentation can be found [here](docs/advanced-usage-option.md) - focusing on async method usage.
