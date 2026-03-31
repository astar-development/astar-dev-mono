using AStar.Dev.OneDriveSync.Features.Onboarding;
using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Features.Onboarding;

public sealed class GivenAUserTypeService
{
    [Fact]
    public void when_service_is_created_then_default_user_type_is_casual()
    {
        var sut = new UserTypeService();

        sut.CurrentUserType.ShouldBe(UserType.Casual);
    }

    [Theory]
    [InlineData(UserType.Casual)]
    [InlineData(UserType.PowerUser)]
    public void when_user_type_is_changed_then_current_user_type_is_updated(UserType newType)
    {
        var sut = new UserTypeService();

        sut.SetUserType(newType);

        sut.CurrentUserType.ShouldBe(newType);
    }

    [Fact]
    public void when_changing_to_power_user_then_confirmation_requested_event_is_raised()
    {
        var sut = new UserTypeService();
        var confirmationRequested = false;
        sut.ConfirmationRequested += (_, _) => confirmationRequested = true;

        sut.RequestUserTypeChange(UserType.PowerUser);

        confirmationRequested.ShouldBeTrue();
    }

    [Fact]
    public void when_changing_to_casual_user_then_no_confirmation_requested_event_is_raised()
    {
        var sut = new UserTypeService();
        sut.SetUserType(UserType.PowerUser);
        var confirmationRequested = false;
        sut.ConfirmationRequested += (_, _) => confirmationRequested = true;

        sut.RequestUserTypeChange(UserType.Casual);

        confirmationRequested.ShouldBeFalse();
    }

    [Fact]
    public void when_confirmation_is_accepted_then_user_type_is_changed()
    {
        var sut = new UserTypeService();

        sut.RequestUserTypeChange(UserType.PowerUser);
        sut.ConfirmUserTypeChange(accepted: true);

        sut.CurrentUserType.ShouldBe(UserType.PowerUser);
    }

    [Fact]
    public void when_confirmation_is_rejected_then_user_type_remains_unchanged()
    {
        var sut = new UserTypeService();

        sut.RequestUserTypeChange(UserType.PowerUser);
        sut.ConfirmUserTypeChange(accepted: false);

        sut.CurrentUserType.ShouldBe(UserType.Casual);
    }
}
