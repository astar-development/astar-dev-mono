namespace AStar.Dev.OneDriveSync.Features.Accounts;

public interface IAccountRepository
{
    Task<bool> HasAnyAsync();
}
