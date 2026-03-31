using AStar.Dev.OneDriveSync.Features.Onboarding;
using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Features.Onboarding;

public sealed class GivenAUserTypeService : IAsyncLifetime
{
    private SqliteConnection? _connection;

    public async ValueTask InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync().ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
        }
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection!)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }

    [Fact]
    public void when_service_is_created_without_saved_settings_then_default_user_type_is_casual()
    {
        using var dbContext = CreateContext();
        var sut = new UserTypeService(dbContext);

        sut.CurrentUserType.ShouldBe(UserType.Casual);
    }

    [Theory]
    [InlineData(UserType.Casual)]
    [InlineData(UserType.PowerUser)]
    public void when_user_type_is_changed_then_current_user_type_is_updated(UserType newType)
    {
        using var dbContext = CreateContext();
        var settings = new AppSettings();
        dbContext.AppSettings.Add(settings);
        _ = dbContext.SaveChanges();

        var sut = new UserTypeService(dbContext);

        sut.SetUserType(newType);

        sut.CurrentUserType.ShouldBe(newType);
    }

    [Fact]
    public void when_changing_to_power_user_then_confirmation_requested_event_is_raised()
    {
        using var dbContext = CreateContext();
        var settings = new AppSettings();
        dbContext.AppSettings.Add(settings);
        _ = dbContext.SaveChanges();

        var sut = new UserTypeService(dbContext);
        var confirmationRequested = false;
        sut.ConfirmationRequested += (_, _) => confirmationRequested = true;

        sut.RequestUserTypeChange(UserType.PowerUser);

        confirmationRequested.ShouldBeTrue();
    }

    [Fact]
    public void when_changing_to_casual_user_then_no_confirmation_requested_event_is_raised()
    {
        using var dbContext = CreateContext();
        var settings = new AppSettings();
        dbContext.AppSettings.Add(settings);
        _ = dbContext.SaveChanges();

        var sut = new UserTypeService(dbContext);
        sut.SetUserType(UserType.PowerUser);
        var confirmationRequested = false;
        sut.ConfirmationRequested += (_, _) => confirmationRequested = true;

        sut.RequestUserTypeChange(UserType.Casual);

        confirmationRequested.ShouldBeFalse();
    }

    [Fact]
    public void when_confirmation_is_accepted_then_user_type_is_changed()
    {
        using var dbContext = CreateContext();
        var settings = new AppSettings();
        dbContext.AppSettings.Add(settings);
        _ = dbContext.SaveChanges();

        var sut = new UserTypeService(dbContext);

        sut.RequestUserTypeChange(UserType.PowerUser);
        sut.ConfirmUserTypeChange(accepted: true);

        sut.CurrentUserType.ShouldBe(UserType.PowerUser);
    }

    [Fact]
    public void when_confirmation_is_rejected_then_user_type_remains_unchanged()
    {
        using var dbContext = CreateContext();
        var settings = new AppSettings();
        dbContext.AppSettings.Add(settings);
        _ = dbContext.SaveChanges();

        var sut = new UserTypeService(dbContext);

        sut.RequestUserTypeChange(UserType.PowerUser);
        sut.ConfirmUserTypeChange(accepted: false);

        sut.CurrentUserType.ShouldBe(UserType.Casual);
    }
}

