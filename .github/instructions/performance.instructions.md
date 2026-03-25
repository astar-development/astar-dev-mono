# Performance Guidelines

Performance best practices and optimization strategies for the AStar Dev OneDrive Sync Client.

---

## Sync Performance

### Delta Queries

- **Strategy**: Only fetch changes since last sync (incremental)
- **Benefit**: Dramatically reduces API calls and data transfer
- **Implementation**: Use Microsoft Graph `/delta` endpoint with stored delta tokens
- **Best Practice**: Always store delta token after successful sync for resumption

### Batch Operations

- **Strategy**: Group multiple API calls into batch requests where possible
- **Benefit**: Reduces network round trips and latency
- **Implementation**: Use Microsoft Graph `$batch` endpoint for multiple operations
- **Best Practice**: Batch up to 20 requests per batch (Graph API limit)

### Progress Throttling

- **Strategy**: Limit progress update frequency to UI
- **Benefit**: Prevents UI flooding and improves responsiveness
- **Implementation**: Use Rx `Throttle()` operator (100ms throttle = max 10 updates/sec)
- **Best Practice**: Report progress per file, not per chunk

### Cancellation Support

- **Strategy**: Support mid-sync cancellation to improve user experience
- **Benefit**: Users can stop long-running syncs without waiting
- **Implementation**: Pass `CancellationToken` through all async methods
- **Best Practice**: Check cancellation token before expensive operations

### Connection Pooling

- **Strategy**: Reuse HTTP connections for Graph API calls
- **Benefit**: Reduces connection establishment overhead
- **Implementation**: Use singleton `HttpClient` with `IHttpClientFactory`
- **Best Practice**: Configure max connections per endpoint based on workload

---

## Memory Management

### Streaming

- **Strategy**: Stream large file uploads/downloads instead of buffering
- **Benefit**: Reduces memory footprint for large files
- **Implementation**: Use `Stream` with chunked reading/writing
- **Best Practice**: Use 64KB-1MB chunk size for optimal throughput

**Example**:

```csharp
await using var fileStream = File.OpenRead(filePath);
await graphClient.Me.Drive.Items[itemId].Content
    .PutAsync(fileStream, cancellationToken);
```

### DbContext Lifetime

- **Strategy**: DbContext scoped to service lifetime (per-operation)
- **Benefit**: Prevents memory leaks from long-lived contexts
- **Implementation**: Register `SyncDbContext` as `Scoped` service
- **Best Practice**: Use `using` blocks or dependency injection for automatic disposal

### File Handles

- **Strategy**: Properly dispose file handles after use
- **Benefit**: Prevents "file in use" errors and resource exhaustion
- **Implementation**: Use `using` statements or `await using` for async disposal
- **Best Practice**: Dispose immediately after use, not at end of method

**Example**:

```csharp
await using var stream = File.OpenRead(filePath);
// Use stream
// Automatically disposed here
```

### Large Collection Processing

- **Strategy**: Process collections in chunks or use streaming
- **Benefit**: Prevents loading entire result set into memory
- **Implementation**: Use `IAsyncEnumerable<T>` or pagination
- **Best Practice**: Yield return items one at a time for streaming

**Example**:

```csharp
public async IAsyncEnumerable<DriveItem> GetAllItemsAsync(
    [EnumeratorCancellation] CancellationToken ct = default)
{
    var request = _client.Me.Drive.Root.Children.Request();

    do
    {
        var page = await request.GetAsync(ct);
        foreach (var item in page)
        {
            yield return item;  // Stream items
        }
        request = page.NextPageRequest;
    } while (request != null);
}
```

---

## Database Performance

### Indexing

- **Strategy**: Add indexes for frequently queried columns
- **Benefit**: Dramatically improves query performance
- **Implementation**: Include indexes in Entity Framework migrations
- **Best Practice**: Index foreign keys, status fields, and timestamp columns

**Example**:

```csharp
modelBuilder.Entity<SyncItem>(entity =>
{
    entity.HasIndex(e => e.AccountId);
    entity.HasIndex(e => e.SyncStatus);
    entity.HasIndex(e => e.LastModifiedTime);
    entity.HasIndex(e => new { e.AccountId, e.SyncStatus }); // Composite index
});
```

### Query Optimization

- **Strategy**: Use `Include()` to eager load related entities
- **Benefit**: Prevents N+1 query problem
- **Implementation**: Include navigation properties in single query
- **Best Practice**: Use `ThenInclude()` for nested relationships

**Example**:

```csharp
var items = await _context.SyncItems
    .Include(i => i.Conflicts)
    .ThenInclude(c => c.ResolutionHistory)
    .Where(i => i.AccountId == accountId)
    .ToListAsync(ct);
```

### Batch Updates

- **Strategy**: Update multiple records in single transaction
- **Benefit**: Reduces database round trips and improves throughput
- **Implementation**: Use `AddRange()` and `SaveChangesAsync()` once
- **Best Practice**: Batch up to 1000 records per transaction

**Example**:

```csharp
var items = await GetItemsToUpdateAsync();
_context.UpdateRange(items);
await _context.SaveChangesAsync(ct);
```

### AsNoTracking for Read-Only Queries

- **Strategy**: Use `AsNoTracking()` for queries that don't need change tracking
- **Benefit**: Reduces memory overhead and improves query performance
- **Implementation**: Add `.AsNoTracking()` to LINQ query
- **Best Practice**: Use for all read-only queries (reporting, display)

**Example**:

```csharp
var items = await _context.SyncItems
    .AsNoTracking()  // Read-only, no change tracking
    .Where(i => i.AccountId == accountId)
    .ToListAsync(ct);
```

### Connection Pooling

- **Strategy**: SQLite connection pooling is automatic
- **Benefit**: Reuses connections across operations
- **Implementation**: Use connection string with `Pooling=True` (default)
- **Best Practice**: Use scoped DbContext to return connections to pool quickly

---

## API Rate Limiting

### Respect Rate Limits

- **Strategy**: Monitor and respect Microsoft Graph API rate limits
- **Benefit**: Prevents throttling and service degradation
- **Implementation**: Check `Retry-After` header in 429 responses
- **Best Practice**: Implement exponential backoff with jitter

**Example**:

```csharp
private async Task<T> ExecuteWithRetryAsync<T>(
    Func<Task<T>> operation,
    CancellationToken ct)
{
    var retryCount = 0;
    var maxRetries = 5;

    while (true)
    {
        try
        {
            return await operation();
        }
        catch (ServiceException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            if (retryCount >= maxRetries) throw;

            var delay = ex.ResponseHeaders.RetryAfter?.Delta
                ?? TimeSpan.FromSeconds(Math.Pow(2, retryCount));

            await Task.Delay(delay, ct);
            retryCount++;
        }
    }
}
```

---

## Caching Strategies

### In-Memory Cache

- **Strategy**: Cache frequently accessed, rarely changing data
- **Benefit**: Reduces database and API calls
- **Implementation**: Use `IMemoryCache` or `BehaviorSubject<T>`
- **Best Practice**: Set appropriate expiration based on data volatility

### Database Cache

- **Strategy**: Cache remote file metadata locally
- **Benefit**: Enables offline conflict detection and faster sync
- **Implementation**: Store DriveItem metadata in `DriveItemsRepository`
- **Best Practice**: Invalidate cache on delta sync (cTag changes)

### Delta Token Cache

- **Strategy**: Always persist delta tokens after successful sync
- **Benefit**: Enables incremental sync and reduces data transfer
- **Implementation**: Store in `SyncRepository` per account
- **Best Practice**: Clear token on full re-sync or account re-authentication

---

## Performance Monitoring

### Key Metrics

- Sync duration (total time)
- Files processed per second
- Bytes transferred per second
- API calls per sync operation
- Database query duration
- Memory usage (peak and average)
- CPU usage during sync

### Optimization Targets

- Sync 100 files in < 30 seconds (typical workload)
- Memory usage < 200MB for 10,000 files
- API calls < 50 for typical sync (delta query)
- Database queries < 5ms per operation
- UI remains responsive during sync (< 100ms frame time)
