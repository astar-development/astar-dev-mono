# C# Implicit Conversion Chains

C# does not chain multiple user-defined implicit conversions automatically. If an expression needs to be converted through two domain types, make the intermediate conversion explicit.

For example, these conversions can be defined independently:

```csharp
ScrapeConfigurationEntity? -> Option<ScrapeConfigurationEntity>
Option<ScrapeConfigurationEntity> -> Exceptional<Option<ScrapeConfigurationEntity>>
```

The first conversion can treat `null` as `Option.None`, while the second lifts the option into a successful `Exceptional` value. An expression-bodied method can use both conversions by explicitly casting to the intermediate type:

```csharp
async Task<Exceptional<Option<ScrapeConfigurationEntity>>> TryFindAsync(
    ScrapeConfigurationId key) =>
    (Option<ScrapeConfigurationEntity>)await ScrapeConfigurations.FindAsync(key);
```

The cast applies the entity-to-option conversion. The method's declared return type then applies the option-to-exceptional conversion, preserving both the concise expression-bodied form and the absence of explicit `Some`/`None` construction.

When an implicit conversion intentionally accepts `null`, its parameter should be annotated accordingly:

```csharp
public static implicit operator Option<T>(T? value) =>
    value != null
        ? new Option<T>.Some(value)
        : Option<T>.None.Instance;
```

This keeps nullable analysis aligned with the conversion's documented behavior.
