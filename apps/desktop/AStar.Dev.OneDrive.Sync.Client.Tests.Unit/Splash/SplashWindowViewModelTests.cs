namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Splash;

public sealed class GivenASplashWindowViewModel
{
    [Fact]
    public void when_created_then_status_is_empty()
    {
        var sut = new AStar.Dev.OneDrive.Sync.Client.Splash.SplashWindowViewModel();

        sut.Status.ShouldBeEmpty();
    }

    [Fact]
    public void when_status_is_set_then_property_changed_is_raised()
    {
        var sut = new AStar.Dev.OneDrive.Sync.Client.Splash.SplashWindowViewModel();
        var raised = false;
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
        var sut = new AStar.Dev.OneDrive.Sync.Client.Splash.SplashWindowViewModel();

        sut.Status = "Configuring database…";

        sut.Status.ShouldBe("Configuring database…");
    }
}
