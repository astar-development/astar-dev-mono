# IDisposable Pattern

Use this pattern for types that own disposable managed resources. The `isDisposing` guard makes repeated calls to `Dispose()` safe.

```csharp
private bool isDisposing;

public void Dispose()
{
    Dispose(true);
    GC.SuppressFinalize(this);
}

private void Dispose(bool disposing)
{
    if (isDisposing) return;

    if (disposing)
    {
        ownedResource?.Dispose();
        ownedResource = null;
    }

    isDisposing = true;
}
```

`Dispose(true)` releases managed resources during explicit disposal. `GC.SuppressFinalize(this)` is required even when the type has no finalizer; it keeps the public implementation correct if one is added later. A finalizer is only needed when the type directly owns unmanaged resources.
