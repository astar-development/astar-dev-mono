using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenAFileTermLinks
{
    [Fact]
    public void when_the_source_list_changes_later_then_the_links_are_unchanged()
    {
        var path = RelativeSyncPathFactory.Create("Photos/a.jpg").Match(created => created, _ => throw new InvalidOperationException());
        var termIds = new List<TermId> { TermIdFactory.Create(Guid.NewGuid()) };
        var links = FileTermLinksFactory.Create(AccountIdFactory.Create(Guid.NewGuid()), path, termIds);

        termIds.Clear();

        links.TermIds.Count.ShouldBe(1);
    }
}
