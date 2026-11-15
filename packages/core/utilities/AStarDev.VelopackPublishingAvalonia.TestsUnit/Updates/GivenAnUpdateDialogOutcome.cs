using AStar.Dev.Velopack.Publishing.Avalonia.Updates;
using Microsoft.Extensions.Logging;
using Velopack;

namespace AStar.Dev.Velopack.Publishing.Avalonia.TestsUnit.Updates;

public sealed class GivenAnUpdateDialogOutcome
{
    private static UpdateInfo CreateUpdateInfo()
    {
        var asset = new VelopackAsset { Version = SemanticVersion.Parse("1.2.3"), NotesMarkdown = "notes" };

        return new UpdateInfo(asset, false, null, []);
    }

    private static UpdateAvailableViewModelFactory CreateFactory(IVelopackUpdateService updateCheckService)
        => new(updateCheckService, new ImmediateUpdateApplyGate(), new FakeUpdateDialogTextProvider(), Substitute.For<ILogger<UpdateAvailableViewModel>>());

    [Fact]
    public void when_nothing_has_been_chosen_then_the_dialog_was_dismissed()
    {
        using var viewModel = CreateFactory(Substitute.For<IVelopackUpdateService>()).Create(CreateUpdateInfo());

        viewModel.Outcome.ShouldBeOfType<Dismissed>();
    }

    [Fact]
    public void when_later_is_chosen_then_the_outcome_is_defer_and_the_dialog_closes()
    {
        using var viewModel = CreateFactory(Substitute.For<IVelopackUpdateService>()).Create(CreateUpdateInfo());
        var closed = false;
        viewModel.CloseRequested += (_, _) => closed = true;

        viewModel.LaterCommand.Execute(null);

        viewModel.Outcome.ShouldBeOfType<Defer>();
        closed.ShouldBeTrue();
    }

    [Fact]
    public async Task when_restart_now_is_chosen_then_the_outcome_is_install()
    {
        using var viewModel = CreateFactory(Substitute.For<IVelopackUpdateService>()).Create(CreateUpdateInfo());

        await viewModel.RestartNowCommand.ExecuteAsync(null);

        viewModel.Outcome.ShouldBeOfType<Install>();
    }

    [Fact]
    public void when_the_update_is_optional_then_the_dialog_can_be_deferred_and_shows_the_normal_message()
    {
        using var viewModel = CreateFactory(Substitute.For<IVelopackUpdateService>()).Create(CreateUpdateInfo());

        viewModel.IsForced.ShouldBeFalse();
        viewModel.CanDefer.ShouldBeTrue();
        viewModel.Message.ShouldBe("Update.Message:1.2.3");
    }

    [Fact]
    public void when_the_update_is_forced_then_the_dialog_cannot_be_deferred_and_shows_the_forced_message()
    {
        using var viewModel = CreateFactory(Substitute.For<IVelopackUpdateService>()).CreateForced(CreateUpdateInfo());

        viewModel.IsForced.ShouldBeTrue();
        viewModel.CanDefer.ShouldBeFalse();
        viewModel.Message.ShouldBe("Update.ForcedMessage:1.2.3");
    }

    [Fact]
    public void when_the_outcome_cases_are_created_then_each_is_its_own_case()
    {
        UpdateDialogOutcomeFactory.CreateInstall().ShouldBeOfType<Install>();
        UpdateDialogOutcomeFactory.CreateDefer().ShouldBeOfType<Defer>();
        UpdateDialogOutcomeFactory.CreateDismissed().ShouldBeOfType<Dismissed>();
    }
}
