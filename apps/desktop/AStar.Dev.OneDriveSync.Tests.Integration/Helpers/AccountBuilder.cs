using System;
using AStar.Dev.OneDriveSync.Features.Accounts;

namespace AStar.Dev.OneDriveSync.Tests.Integration.Helpers;

internal sealed class AccountBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _displayName = "Test User";
    private string _email = "test@example.com";
    private string _microsoftAccountId = Guid.NewGuid().ToString();

    public AccountBuilder WithId(Guid id)
    {
        _id = id;

        return this;
    }

    public AccountBuilder WithDisplayName(string displayName)
    {
        _displayName = displayName;

        return this;
    }

    public AccountBuilder WithEmail(string email)
    {
        _email = email;

        return this;
    }

    public AccountBuilder WithMicrosoftAccountId(string msId)
    {
        _microsoftAccountId = msId;

        return this;
    }

    public Account Build() => new()
    {
        Id                 = _id,
        DisplayName        = _displayName,
        Email              = _email,
        MicrosoftAccountId = _microsoftAccountId
    };
}
