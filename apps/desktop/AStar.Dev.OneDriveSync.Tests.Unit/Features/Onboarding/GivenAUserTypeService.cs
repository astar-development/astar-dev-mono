using System.Threading.Tasks;
using AStar.Dev.OneDriveSync.Features.Onboarding;
using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Features.Onboarding;

public sealed class GivenAUserTypeService : IAsyncLifetime
{
    private SqliteConnection? _connection;
    private InMemoryDbContextFactory? _contextFactory;

    public async ValueTask InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync().ConfigureAwait(false);

        _contextFactory = new InMemoryDbContextFactory(_connection);
        await using var ctx = _contextFactory.CreateDbContext();
        await ctx.Database.MigrateAsync().ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync().ConfigureAwait(false);
    }

    [Fact]
    public void when_service_is_created_without_saved_settings_then_default_user_type_is_casual()
    {
        var sut = new UserTypeService(_contextFactory!);

        sut.CurrentUserType.ShouldBe(UserType.Casual);
    }

    [Theory]
    [InlineData(UserType.Casual)]
    [InlineData(UserType.PowerUser)]
    public void when_user_type_is_changed_then_current_user_type_is_updated(UserType newType)
    {
        SeedAppSettings();

        var sut = new UserTypeService(_contextFactory!);

        sut.SetUserType(newType);

        sut.CurrentUserType.ShouldBe(newType);
    }

    [Fact]
    public void when_changing_to_power_user_then_confirmation_requested_event_is_raised()
    {
        SeedAppSettings();

        var sut = new UserTypeService(_contextFactory!);
        bool confirmationRequested = false;
        sut.ConfirmationRequested += (_, _) => confirmationRequested = true;

        sut.RequestUserTypeChange(UserType.PowerUser);

        confirmationRequested.ShouldBeTrue();
    }

    [Fact]
    public void when_changing_to_casual_user_then_no_confirmation_requested_event_is_raised()
    {
        SeedAppSettings();

        var sut = new UserTypeService(_contextFactory!);
        sut.SetUserType(UserType.PowerUser);
        bool confirmationRequested = false;
        sut.ConfirmationRequested += (_, _) => confirmationRequested = true;

        sut.RequestUserTypeChange(UserType.Casual);

        confirmationRequested.ShouldBeFalse();
    }

    [Fact]
    public void when_confirmation_is_accepted_then_user_type_is_changed()
    {
        SeedAppSettings();

        var sut = new UserTypeService(_contextFactory!);

        sut.RequestUserTypeChange(UserType.PowerUser);
        sut.ConfirmUserTypeChange(accepted: true);

        sut.CurrentUserType.ShouldBe(UserType.PowerUser);
    }

    [Fact]
    public void when_confirmation_is_rejected_then_user_type_remains_unchanged()
    {
        SeedAppSettings();

        var sut = new UserTypeService(_contextFactory!);

        sut.RequestUserTypeChange(UserType.PowerUser);
        sut.ConfirmUserTypeChange(accepted: false);

        sut.CurrentUserType.ShouldBe(UserType.Casual);
    }

    private void SeedAppSettings()
    {
        using var ctx = _contextFactory!.CreateDbContext();
        ctx.AppSettings.Add(new AppSettings());
        _ = ctx.SaveChanges();
    }
}

internal sealed class InMemoryDbContextFactory(SqliteConnection connection) : IDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        return new AppDbContext(options);
    }
}
