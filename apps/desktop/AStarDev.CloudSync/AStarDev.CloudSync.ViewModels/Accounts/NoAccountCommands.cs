namespace AStarDev.CloudSync.ViewModels.Accounts;

/// <summary>Account commands that do nothing, for views built without the account dialog (previews and older tests).</summary>
public sealed class NoAccountCommands : IAccountCommands
{
    private NoAccountCommands()
    {
    }

    /// <summary>The shared instance.</summary>
    public static NoAccountCommands Instance { get; } = new();

    /// <inheritdoc />
    public Task SignInAsync(AccountViewModel account, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task RemoveAsync(AccountViewModel account, CancellationToken cancellationToken) => Task.CompletedTask;
}
