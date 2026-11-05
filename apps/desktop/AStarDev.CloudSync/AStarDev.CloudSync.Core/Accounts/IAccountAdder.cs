namespace AStarDev.CloudSync.Core.Accounts;

/// <summary>Signs the user in to a cloud account, reads its drive and saves it.</summary>
public interface IAccountAdder
{
    /// <summary>Runs one add-account attempt from sign-in to save.</summary>
    /// <param name="cancellationToken">Cancels the attempt; the outcome is then <see cref="AccountAddOutcome.Cancelled"/>.</param>
    /// <returns>The outcome of the attempt. It never throws for a sign-in, drive or save failure.</returns>
    Task<AccountAddOutcome> AddAsync(CancellationToken cancellationToken);
}
