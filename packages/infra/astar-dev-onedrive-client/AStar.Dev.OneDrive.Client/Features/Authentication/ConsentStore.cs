using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Client.Features.Authentication;

/// <summary>
///     In-memory consent store for tokens (AU-02, AU-03).
///     Desktop app will override with database-backed implementation.
/// </summary>
internal sealed class ConsentStore : IConsentStore
{
    private readonly Dictionary<Guid, bool> _consentDecisions = new();

    public Task<Option<bool>> GetConsentDecisionAsync(Guid accountId, CancellationToken ct = default)
    {
        lock (_consentDecisions)
        {
            var result = _consentDecisions.TryGetValue(accountId, out var consented)
                ? (Option<bool>)new Option<bool>.Some(consented)
                : Option<bool>.None.Instance;

            return Task.FromResult(result);
        }
    }

    public Task<Result<bool, string>> SetConsentDecisionAsync(Guid accountId, bool consented, CancellationToken ct = default)
    {
        lock (_consentDecisions)
        {
            _consentDecisions[accountId] = consented;
        }

        return Task.FromResult((Result<bool, string>)new Result<bool, string>.Ok(consented));
    }
}
