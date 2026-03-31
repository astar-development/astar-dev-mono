namespace AStar.Dev.OneDriveSync.Features.Onboarding;

public interface IUserTypeService
{
    UserType CurrentUserType { get; }

    event EventHandler<EventArgs>? ConfirmationRequested;

    void RequestUserTypeChange(UserType userType);

    void ConfirmUserTypeChange(bool accepted);

    void SetUserType(UserType userType);
}
