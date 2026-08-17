// using System.Reflection;
// using JetBrains.Annotations;

// namespace AStar.Dev.Technical.Debt.Reporting;

// [TestSubject(typeof(Reporter))]
// public class ReporterShould
// {
//     private readonly Assembly assemblyToReportOn;

//     public ReporterShould()
//     {
//         var executingDirectory = AppDomain.CurrentDomain.BaseDirectory;
//         var root               = Path.Combine(executingDirectory, "..\\", "..\\", "..\\", "..\\");
//         var dllLocation        = Path.Combine(root,               "DemoClassLibrary\\bin\\Debug\\net9.0\\DemoClassLibrary.dll");

//         assemblyToReportOn =  Assembly.LoadFile(dllLocation);
//     }

//     [Fact]
//     public void GenerateReportAsExpected()
//     {
//         var report = Reporter.GenerateReport([assemblyToReportOn]);

//         report.ShouldStartWith("***Start of Refactorings Report");
//         report.ShouldContain("Relative Benefit to fix: 1 Empty class... DemoClassLibrary.Class1 ( - ) Pain: 1 Effort to fix: 1");
//         report.ShouldContain("***End of Refactorings Report.");
//     }

//     [Fact]
//     public void AssertMaxPainNotExceededThrowsWhenExpected()
//     {
//         var act = () => Reporter.AssertMaxPainNotExceeded(assemblyToReportOn, 2);

//         act.ShouldThrow<TechDebtPainExceededException>();
//     }

//     [Fact]
//     public void AssertMaxPainNotExceededDoesNotThrowWhenExpected()
//     {
//         var act = () => Reporter.AssertMaxPainNotExceeded(assemblyToReportOn, 100);

//         act.ShouldNotThrow();
//     }
// }
