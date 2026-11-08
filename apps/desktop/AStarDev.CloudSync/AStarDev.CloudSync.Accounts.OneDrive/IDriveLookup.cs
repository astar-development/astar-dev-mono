using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Accounts.OneDrive;

/// <summary>Reads the signed-in account's OneDrive drive.</summary>
public interface IDriveLookup
{
    /// <summary>Reads the drive id and quota through <c>GET /me/drive</c>.</summary>
    /// <param name="accessToken">The access token from the sign-in.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The drive, or <see cref="AccountAddFailure.Network"/> or <see cref="AccountAddFailure.DriveUnavailable"/>.</returns>
    Task<Result<MicrosoftDrive, AccountAddFailure>> GetDriveAsync(string accessToken, CancellationToken cancellationToken);
}
