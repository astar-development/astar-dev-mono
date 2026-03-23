namespace AStar.Dev.Test.Helpers;

internal class MockClass
{
    public DateTime IrrelevantDate { get; set; } =  DateTime.UtcNow;

    public Guid IrrelevantGuid { get; set; } = Guid.CreateVersion7();

    public string IrrelevantString { get; set; } = "Irrelevant string";
}