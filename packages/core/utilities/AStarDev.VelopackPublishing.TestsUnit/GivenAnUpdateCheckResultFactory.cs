using Velopack;

namespace AStar.Dev.Velopack.Publishing.TestsUnit;

public sealed class GivenAnUpdateCheckResultFactory
{
    private static UpdateInfo CreateUpdateInfo()
    {
        var asset = new VelopackAsset { Version = SemanticVersion.Parse("1.2.3") };

        return new UpdateInfo(asset, false, null, []);
    }

    [Fact]
    public void when_found_is_created_with_update_info_then_it_carries_the_update_info()
    {
        var updateInfo = CreateUpdateInfo();

        var result = UpdateCheckResultFactory.CreateFound(updateInfo);

        result.ShouldBeOfType<UpdateFound>().Info.ShouldBe(updateInfo);
    }

    [Fact]
    public void when_found_is_created_with_null_update_info_then_check_failed_is_returned()
    {
        var result = UpdateCheckResultFactory.CreateFound(null!);

        result.ShouldBeOfType<CheckFailed>();
    }

    [Fact]
    public void when_up_to_date_is_created_then_it_is_up_to_date()
    {
        var result = UpdateCheckResultFactory.CreateUpToDate();

        result.ShouldBeOfType<UpToDate>();
    }

    [Fact]
    public void when_not_installed_is_created_then_it_is_not_installed()
    {
        var result = UpdateCheckResultFactory.CreateNotInstalled();

        result.ShouldBeOfType<NotInstalled>();
    }

    [Fact]
    public void when_failed_is_created_with_a_reason_then_it_carries_the_reason()
    {
        var result = UpdateCheckResultFactory.CreateFailed("boom");

        result.ShouldBeOfType<CheckFailed>().Reason.ShouldBe("boom");
    }

    [Fact]
    public void when_failed_is_created_with_a_blank_reason_then_a_default_reason_is_used()
    {
        var result = UpdateCheckResultFactory.CreateFailed(" ");

        result.ShouldBeOfType<CheckFailed>().Reason.ShouldNotBeNullOrWhiteSpace();
    }
}
