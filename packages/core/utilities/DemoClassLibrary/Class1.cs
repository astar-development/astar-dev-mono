using AStar.Dev.Technical.Debt.Reporting;

namespace DemoClassLibrary;

[Refactor(1,1,"Empty class...")]
public class Class1
{
    [Refactor(1, 1, "Empty method...")]
#pragma warning disable CA1822
    public void Method1()
#pragma warning restore CA1822
    {
        // NAR
    }

    [Refactor(1, 1, "Unused property...")]
    public int SomeUnusedInteger { get; set; }
}
