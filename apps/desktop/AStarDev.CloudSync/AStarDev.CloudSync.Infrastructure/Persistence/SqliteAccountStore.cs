using AStarDev.CloudSync.Core.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Infrastructure.Persistence;

/// <summary>Keeps accounts in the CloudSync SQLite database.</summary>
/// <param name="dbContextFactory">Creates a database context per operation.</param>
/// <param name="logger">Reports saves and reads that fail.</param>
public sealed class SqliteAccountStore(IDbContextFactory<CloudSyncDbContext> dbContextFactory, ILogger<SqliteAccountStore> logger) : IAccountStore
{
    /// <inheritdoc />
    public Task<AccountAddOutcome> AddAsync(AccountRegistration registration, CancellationToken cancellationToken) => throw new NotImplementedException();

    /// <inheritdoc />
    public Task<Result<Option<StoredAccount>, ErrorResponse>> GetByKeyAsync(CloudProvider provider, ProviderAccountKey key, CancellationToken cancellationToken) => throw new NotImplementedException();

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<StoredAccount>, ErrorResponse>> GetAllAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"{dbContextFactory}{logger}");
}
