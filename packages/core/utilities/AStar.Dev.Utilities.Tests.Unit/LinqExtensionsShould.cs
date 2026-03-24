namespace AStar.Dev.Utilities.Tests.Unit;

public class LinqExtensionsShould
{
    [Fact]
    public void ContainTheForEachExtensionOnIEnumerableAndTriggerTheSuppliedAction()
    {
        Exception? exception = Record.Exception(() => new List<string> { "", "z", "a" }.AsEnumerable().ForEach(_ => { }));

        Assert.Null(exception);
    }
}