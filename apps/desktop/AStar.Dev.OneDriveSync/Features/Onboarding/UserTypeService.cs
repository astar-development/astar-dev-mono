using AStar.Dev.OneDriveSync.Infrastructure.Persistence;

namespace AStar.Dev.OneDriveSync.Features.Onboarding;

internal sealed class UserTypeService(IAppDbContext dbContext) : IUserTypeService
{
    private readonly IAppDbContext _dbContext = dbContext;
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
        if (userType == UserType.PowerUser && CurrentUserType != UserType.PowerUser)
        {
            _pendingUserType = userType;
            ConfirmationRequested?.Invoke(this, EventArgs.Empty);
        }
        else if (userType == UserType.Casual)
        {
            SetUserType(userType);
        }
    }

    public void ConfirmUserTypeChange(bool accepted)
    {
        if (accepted)
        {
            SetUserType(_pendingUserType);
        }
    }

    private void LoadUserType()
    {
        var settings = _dbContext.AppSettings.FirstOrDefault();

        if (settings != null && Enum.TryParse<UserType>(settings.UserType, out var userType))
        {
            _currentUserType = userType;
        }
        else
        {
            _currentUserType = UserType.Casual;
        }

        _initialized = true;
    }

    private void PersistUserType()
    {
        var settings = _dbContext.AppSettings.FirstOrDefault();

        if (settings != null)
        {
            settings.UserType = _currentUserType.ToString();
            _ = _dbContext.SaveChanges();
        }
    }
}
