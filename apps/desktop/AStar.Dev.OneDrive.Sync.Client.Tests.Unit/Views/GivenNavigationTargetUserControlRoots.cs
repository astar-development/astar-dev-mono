using Avalonia.Controls;
using Avalonia.Layout;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Dashboard;
using AStar.Dev.OneDrive.Sync.Client.Home;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Views;

public sealed class GivenNavigationTargetUserControlRoots
{
    [AvaloniaFact]
    public void when_dashboard_view_root_is_inspected_then_vertical_content_alignment_is_stretch()
    {
        var sut = new DashboardView();

        sut.VerticalContentAlignment.ShouldBe(VerticalAlignment.Stretch);
    }

    [AvaloniaFact]
    public void when_dashboard_view_root_is_inspected_then_horizontal_content_alignment_is_stretch()
    {
        var sut = new DashboardView();

        sut.HorizontalContentAlignment.ShouldBe(HorizontalAlignment.Stretch);
    }

    [AvaloniaFact]
    public void when_files_view_root_is_inspected_then_vertical_content_alignment_is_stretch()
    {
        var sut = new FilesView();

        sut.VerticalContentAlignment.ShouldBe(VerticalAlignment.Stretch);
    }

    [AvaloniaFact]
    public void when_files_view_root_is_inspected_then_horizontal_content_alignment_is_stretch()
    {
        var sut = new FilesView();

        sut.HorizontalContentAlignment.ShouldBe(HorizontalAlignment.Stretch);
    }

    [AvaloniaFact]
    public void when_accounts_view_root_is_inspected_then_vertical_content_alignment_is_stretch()
    {
        var sut = new AccountsView();

        sut.VerticalContentAlignment.ShouldBe(VerticalAlignment.Stretch);
    }

    [AvaloniaFact]
    public void when_accounts_view_root_is_inspected_then_horizontal_content_alignment_is_stretch()
    {
        var sut = new AccountsView();

        sut.HorizontalContentAlignment.ShouldBe(HorizontalAlignment.Stretch);
    }

    [AvaloniaFact]
    public void when_activity_view_root_is_inspected_then_vertical_content_alignment_is_stretch()
    {
        var sut = new ActivityView();

        sut.VerticalContentAlignment.ShouldBe(VerticalAlignment.Stretch);
    }

    [AvaloniaFact]
    public void when_activity_view_root_is_inspected_then_horizontal_content_alignment_is_stretch()
    {
        var sut = new ActivityView();

        sut.HorizontalContentAlignment.ShouldBe(HorizontalAlignment.Stretch);
    }
}
