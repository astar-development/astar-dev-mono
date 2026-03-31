namespace AStar.Dev.OneDriveSync.Features.Onboarding;

public sealed class UserTypeService
{
    public UserType CurrentUserType { get; private set; } = UserType.Casual;

    public event EventHandler<EventArgs>? ConfirmationRequested;

    private UserType _pendingUserType = UserType.Casual;

    public void SetUserType(UserType userType)
    {
        CurrentUserType = userType;
    }

    public void RequestUserTypeChange(UserType userType)
    {
        if (userType == UserType.PowerUser && CurrentUserType != UserType.PowerUser)
        {
            _pendingUserType = userType;
            ConfirmationRequested?.Invoke(this, EventArgs.Empty);
        }
        else if (userType == UserType.Casual)
        {
            CurrentUserType = userType;
        }
    }

    public void ConfirmUserTypeChange(bool accepted)
    {
        if (accepted)
        {
            CurrentUserType = _pendingUserType;
        }
    }
}
