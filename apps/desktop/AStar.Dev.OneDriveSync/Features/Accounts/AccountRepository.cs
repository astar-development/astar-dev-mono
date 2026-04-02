using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDriveSync.Features.Accounts;

internal sealed class AccountRepository(AppDbContext dbContext) : IAccountRepository
{
    public async Task<bool> HasAnyAsync() => await dbContext.Accounts.AnyAsync().ConfigureAwait(false);
}
