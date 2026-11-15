using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.ViewModels.Conflicts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Conflicts;
using AStarDev.CloudSync.TestHelpers.Overview;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Conflicts;

public class GivenAConflictListFactory
{
    private static readonly CloudAccount Photos = AttentionItems.Account("Photos");
    private static readonly CloudAccount Work = AttentionItems.Account("Work");

    [Fact]
    public async Task when_created_without_an_account_then_every_accounts_conflicts_are_listed()
    {
        var factory = CreateFactory(out _);

        var list = factory.Create(Option.None<AccountId>());
        await list.LoadAsync(TestContext.Current.CancellationToken);

        list.Groups.Select(group => group.HeaderText).ShouldBe(["Photos · 1 conflict", "Work · 1 conflict"]);
    }

    [Fact]
    public async Task when_created_for_an_account_then_only_that_accounts_conflicts_are_listed()
    {
        var factory = CreateFactory(out _);

        var list = factory.Create(Option.Some(Work.Id));
        await list.LoadAsync(TestContext.Current.CancellationToken);

        list.Groups.Select(group => group.HeaderText).ShouldBe(["Work · 1 conflict"]);
    }

    [Fact]
    public async Task when_created_twice_then_the_lists_are_independent_and_not_open()
    {
        var factory = CreateFactory(out _);

        var first = factory.Create(Option.None<AccountId>());
        var second = factory.Create(Option.None<AccountId>());
        await first.LoadAsync(TestContext.Current.CancellationToken);

        second.ShouldNotBeSameAs(first);
        second.Groups.ShouldBeEmpty();
        (first.IsOpen, second.IsOpen).ShouldBe((false, false));
    }

    private static IConflictListFactory CreateFactory(out FakeAttentionReader reader)
    {
        reader = new FakeAttentionReader();
        reader.Add(Photos.Id, AttentionItems.CreateConflict(Photos.Id, "Camera/1.jpg"));
        reader.Add(Work.Id, AttentionItems.CreateConflict(Work.Id, "Docs/a.docx"));

        return TestData.CreateConflictListFactory(new FixedAccountsSource(Photos, Work), reader);
    }
}
