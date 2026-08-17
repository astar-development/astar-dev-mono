namespace AStarDev.OneDriveSyncClient.TestsUnit.Splash;

public sealed class GivenASplashWindowViewModel
{
    [Fact]
    public void when_created_then_status_is_empty()
    {
        var sut = new AStarDev.OneDriveSyncClient.Splash.SplashWindowViewModel();

        sut.Status.ShouldBeEmpty();
    }

    [Fact]
    public void when_status_is_set_then_property_changed_is_raised()
    {
        var sut = new AStarDev.OneDriveSyncClient.Splash.SplashWindowViewModel();
        bool raised = false;
        sut.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(sut.Status))
                raised = true;
        };

        sut.Status = "Configuring services…";

        raised.ShouldBeTrue();
    }

    [Fact]
    public void when_status_is_set_then_new_value_is_returned()
    {
        var sut = new AStarDev.OneDriveSyncClient.Splash.SplashWindowViewModel
        {
            Status = "Configuring database…"
        };

        sut.Status.ShouldBe("Configuring database…");
    }

    [Fact]
    public void when_created_then_app_name_is_empty() =>
        new AStarDev.OneDriveSyncClient.Splash.SplashWindowViewModel().AppName.ShouldBeEmpty();

    [Fact]
    public void when_app_name_is_initialised_with_a_value_then_app_name_matches()
    {
        var sut = new AStarDev.OneDriveSyncClient.Splash.SplashWindowViewModel { AppName = "My App" };

        sut.AppName.ShouldBe("My App");
    }
}
