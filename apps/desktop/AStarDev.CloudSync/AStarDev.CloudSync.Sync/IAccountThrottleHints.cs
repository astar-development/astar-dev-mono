using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>Tells whether an account has been throttled often enough to suggest a lower concurrency (#1463).</summary>
public interface IAccountThrottleHints
{
    /// <summary>Suggests a lower concurrency when the account was throttled <see cref="ThrottlePolicy.HintThrottleCount"/> times within <see cref="ThrottlePolicy.HintWindow"/>. A throttle reported while the account is already paused is the same throttle and is not counted again. It follows this app session only.</summary>
    /// <param name="accountId">The account.</param>
    ThrottleHint HintFor(AccountId accountId);
}
