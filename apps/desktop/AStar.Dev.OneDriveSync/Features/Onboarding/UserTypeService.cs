using System;
using System.Linq;
using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDriveSync.Features.Onboarding;

internal sealed class UserTypeService(IDbContextFactory<AppDbContext> contextFactory) : IUserTypeService
{
    private UserType _currentUserType;
    private bool _initialized;

    public UserType CurrentUserType
    {
        get
        {
            if (!_initialized)
                LoadUserType();

            return _currentUserType;
        }
    }

    public event EventHandler<EventArgs>? ConfirmationRequested;

    private UserType _pendingUserType = UserType.Casual;

    public void SetUserType(UserType userType)
    {
        _currentUserType = userType;
        _initialized = true;
        PersistUserType();
    }

    public void RequestUserTypeChange(UserType userType)
    {
        switch (userType)
        {
            case UserType.PowerUser when CurrentUserType != UserType.PowerUser:
                _pendingUserType = userType;
                ConfirmationRequested?.Invoke(this, EventArgs.Empty);
                break;
            case UserType.Casual:
                SetUserType(userType);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(userType), userType, null);
        }
    }

    public void ConfirmUserTypeChange(bool accepted)
    {
        if (accepted) SetUserType(_pendingUserType);
    }

    private void LoadUserType()
    {
        using var ctx = contextFactory.CreateDbContext();
        var settings = ctx.AppSettings.FirstOrDefault();

        _currentUserType = settings != null && Enum.TryParse<UserType>(settings.UserType, out var userType)
            ? userType
            : UserType.Casual;

        _initialized = true;
    }

    private void PersistUserType()
    {
        using var ctx = contextFactory.CreateDbContext();
        var settings = ctx.AppSettings.FirstOrDefault();

        if (settings is null)
            return;

        settings.UserType = _currentUserType.ToString();
        _ = ctx.SaveChanges();
    }
}
