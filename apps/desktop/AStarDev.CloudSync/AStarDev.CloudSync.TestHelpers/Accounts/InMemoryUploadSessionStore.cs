using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class InMemoryUploadSessionStore : IUploadSessionStore
{
    private readonly Lock gate = new();
    private readonly Dictionary<(AccountId, string), UploadSession> sessions = [];
    private int saves;
    private int removals;

    public bool Fails { get; set; }

    public int Saves => Volatile.Read(ref saves);

    public int Removals => Volatile.Read(ref removals);

    public IReadOnlyDictionary<(AccountId, string), UploadSession> Sessions
    {
        get
        {
            lock (gate)
                return new Dictionary<(AccountId, string), UploadSession>(sessions);
        }
    }

    public Task<Result<Unit, ErrorResponse>> SaveAsync(AccountId accountId, IFileInfo source, UploadSession session, CancellationToken cancellationToken)
    {
        if (Fails)
            return Task.FromResult(Result.Failure<Unit, ErrorResponse>(new ErrorResponse("locked")));

        lock (gate)
            sessions[(accountId, Key(source))] = session;

        _ = Interlocked.Increment(ref saves);

        return Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance));
    }

    public Task<Result<Option<UploadSession>, ErrorResponse>> GetAsync(AccountId accountId, IFileInfo source, CancellationToken cancellationToken)
    {
        if (Fails)
            return Task.FromResult(Result.Failure<Option<UploadSession>, ErrorResponse>(new ErrorResponse("locked")));

        lock (gate)
            return Task.FromResult(Result.Success<Option<UploadSession>, ErrorResponse>(sessions.TryGetValue((accountId, Key(source)), out var found) ? Option.Some(found) : Option.None<UploadSession>()));
    }

    public Task<Result<int, ErrorResponse>> RemoveExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (gate)
        {
            var expired = sessions.Where(pair => pair.Value.ExpiresAt <= now).Select(pair => pair.Key).ToList();
            foreach (var key in expired)
                _ = sessions.Remove(key);

            return Task.FromResult(Result.Success<int, ErrorResponse>(expired.Count));
        }
    }

    public Task<Result<Unit, ErrorResponse>> RemoveAsync(AccountId accountId, IFileInfo source, CancellationToken cancellationToken)
    {
        if (Fails)
            return Task.FromResult(Result.Failure<Unit, ErrorResponse>(new ErrorResponse("locked")));

        lock (gate)
            _ = sessions.Remove((accountId, Key(source)));

        _ = Interlocked.Increment(ref removals);

        return Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance));
    }

    public void Seed(AccountId accountId, IFileInfo source, UploadSession session)
    {
        lock (gate)
            sessions[(accountId, Key(source))] = session;
    }

    private static string Key(IFileInfo source) => source.FullName.ToUpperInvariant();
}
