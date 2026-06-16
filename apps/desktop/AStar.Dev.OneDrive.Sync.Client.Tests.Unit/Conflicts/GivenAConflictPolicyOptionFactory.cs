using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Localization;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Conflicts;

public sealed class GivenAConflictPolicyOptionFactory
{
    private static ILocalizationService BuildLocalizationService()
    {
        var loc = Substitute.For<ILocalizationService>();
        loc.GetLocal(Arg.Any<string>()).Returns(x => x.ArgAt<string>(0));

        return loc;
    }

    [Fact]
    public void when_create_is_called_then_five_options_are_returned()
    {
        var options = ConflictPolicyOptionFactory.Create(BuildLocalizationService(), ConflictPolicy.Ignore);

        options.Count.ShouldBe(5);
    }

    [Fact]
    public void when_create_is_called_then_all_conflict_policy_values_are_represented()
    {
        var options = ConflictPolicyOptionFactory.Create(BuildLocalizationService(), ConflictPolicy.Ignore);

        var policies = options.Select(option => option.Policy).ToList();
        policies.ShouldContain(ConflictPolicy.Ignore);
        policies.ShouldContain(ConflictPolicy.KeepBoth);
        policies.ShouldContain(ConflictPolicy.LastWriteWins);
        policies.ShouldContain(ConflictPolicy.LocalWins);
        policies.ShouldContain(ConflictPolicy.RemoteWins);
    }

    [Fact]
    public void when_create_is_called_then_label_keys_are_fetched_from_localisation_service()
    {
        var loc = BuildLocalizationService();

        _ = ConflictPolicyOptionFactory.Create(loc, ConflictPolicy.Ignore);

        loc.Received(1).GetLocal("ConflictPolicy.Ignore");
        loc.Received(1).GetLocal("ConflictPolicy.KeepBoth");
        loc.Received(1).GetLocal("ConflictPolicy.LastWriteWins");
        loc.Received(1).GetLocal("ConflictPolicy.LocalWins");
        loc.Received(1).GetLocal("ConflictPolicy.RemoteWins");
    }

    [Fact]
    public void when_create_is_called_then_description_keys_are_fetched_from_localisation_service()
    {
        var loc = BuildLocalizationService();

        _ = ConflictPolicyOptionFactory.Create(loc, ConflictPolicy.Ignore);

        loc.Received(1).GetLocal("ConflictPolicy.Ignore.Description");
        loc.Received(1).GetLocal("ConflictPolicy.KeepBoth.Description");
        loc.Received(1).GetLocal("ConflictPolicy.LastWriteWins.Description");
        loc.Received(1).GetLocal("ConflictPolicy.LocalWins.Description");
        loc.Received(1).GetLocal("ConflictPolicy.RemoteWins.Description");
    }

    [Fact]
    public void when_create_is_called_then_returned_labels_match_localisation_service_output()
    {
        var loc = Substitute.For<ILocalizationService>();
        loc.GetLocal("ConflictPolicy.Ignore").Returns("Ignorer");
        loc.GetLocal("ConflictPolicy.KeepBoth").Returns("Garder les deux");
        loc.GetLocal("ConflictPolicy.LastWriteWins").Returns("Dernière écriture gagne");
        loc.GetLocal("ConflictPolicy.LocalWins").Returns("Local gagne");
        loc.GetLocal("ConflictPolicy.RemoteWins").Returns("Distant gagne");

        var options = ConflictPolicyOptionFactory.Create(loc, ConflictPolicy.Ignore);

        options.Single(option => option.Policy == ConflictPolicy.Ignore).Label.ShouldBe("Ignorer");
        options.Single(option => option.Policy == ConflictPolicy.KeepBoth).Label.ShouldBe("Garder les deux");
        options.Single(option => option.Policy == ConflictPolicy.LastWriteWins).Label.ShouldBe("Dernière écriture gagne");
        options.Single(option => option.Policy == ConflictPolicy.LocalWins).Label.ShouldBe("Local gagne");
        options.Single(option => option.Policy == ConflictPolicy.RemoteWins).Label.ShouldBe("Distant gagne");
    }

    [Fact]
    public void when_create_is_called_then_returned_descriptions_match_localisation_service_output()
    {
        var loc = Substitute.For<ILocalizationService>();
        loc.GetLocal("ConflictPolicy.Ignore.Description").Returns("Ignorer la description");
        loc.GetLocal("ConflictPolicy.KeepBoth.Description").Returns("Garder les deux description");
        loc.GetLocal("ConflictPolicy.LastWriteWins.Description").Returns("Dernière écriture description");
        loc.GetLocal("ConflictPolicy.LocalWins.Description").Returns("Local description");
        loc.GetLocal("ConflictPolicy.RemoteWins.Description").Returns("Distant description");

        var options = ConflictPolicyOptionFactory.Create(loc, ConflictPolicy.Ignore);

        options.Single(option => option.Policy == ConflictPolicy.Ignore).Description.ShouldBe("Ignorer la description");
        options.Single(option => option.Policy == ConflictPolicy.KeepBoth).Description.ShouldBe("Garder les deux description");
        options.Single(option => option.Policy == ConflictPolicy.LastWriteWins).Description.ShouldBe("Dernière écriture description");
        options.Single(option => option.Policy == ConflictPolicy.LocalWins).Description.ShouldBe("Local description");
        options.Single(option => option.Policy == ConflictPolicy.RemoteWins).Description.ShouldBe("Distant description");
    }

    [Fact]
    public void when_selected_policy_is_ignore_then_ignore_option_is_selected()
    {
        var options = ConflictPolicyOptionFactory.Create(BuildLocalizationService(), ConflictPolicy.Ignore);

        options.Single(option => option.Policy == ConflictPolicy.Ignore).IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void when_selected_policy_is_ignore_then_other_options_are_not_selected()
    {
        var options = ConflictPolicyOptionFactory.Create(BuildLocalizationService(), ConflictPolicy.Ignore);

        options.Where(option => option.Policy != ConflictPolicy.Ignore).ShouldAllBe(option => !option.IsSelected);
    }

    [Fact]
    public void when_selected_policy_is_local_wins_then_local_wins_option_is_selected()
    {
        var options = ConflictPolicyOptionFactory.Create(BuildLocalizationService(), ConflictPolicy.LocalWins);

        options.Single(option => option.Policy == ConflictPolicy.LocalWins).IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void when_selected_policy_is_remote_wins_then_remote_wins_option_is_selected()
    {
        var options = ConflictPolicyOptionFactory.Create(BuildLocalizationService(), ConflictPolicy.RemoteWins);

        options.Single(option => option.Policy == ConflictPolicy.RemoteWins).IsSelected.ShouldBeTrue();
    }
}
