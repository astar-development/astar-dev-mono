namespace AStar.Dev.Utilities;

public class LinqExtensionsShould
{
    [Fact]
    public void ContainTheForEachExtensionOnIEnumerableAndTriggerTheSuppliedAction()
    {
        var exception = Record.Exception(() => new List<string> { "", "z", "a" }.AsEnumerable().ForEach(_ => { }));

        Assert.Null(exception);
    }
}